using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace V10Bulldoze
{
    public class UserInterface
    {
        private GameObject abandonedButton, burnedButton, audioButton;
		public XmlHolder data;
		private bool needSave = false;
		private static UserInterface instance;
		public static float[] bulldozeAudioClip = null;
		private static int buttonSize = 80;
		
		public UserInterface ()
		{
			instance = this;
			
			UIView view = GameObject.FindObjectOfType<UIView> ();

			if (view == null)
				return;
			
			UIComponent bulldozerBar = UIView.Find ("BulldozerBar");
			
			if (bulldozerBar == null)
				return;
			
			try {
				XmlSerializer serializer = new XmlSerializer (typeof(XmlHolder));
				using (StreamReader reader = new StreamReader("V10Bulldoze.xml")) {
					data = (XmlHolder)serializer.Deserialize (reader);
					reader.Close ();
				}
			} catch (FileNotFoundException) {
				// No options file yet
				data = new XmlHolder ();
			} catch (Exception e) {
				Debug.Log ("V10Bulldoze: " + e.GetType ().Name + " while reading xml file: " + e.Message + "\n" + e.StackTrace + "\n\n");
				if (e.InnerException != null) 
					Debug.Log ("Caused by: " + e.InnerException.GetType ().Name + ": " + e.InnerException.Message + "\n" + e.InnerException.StackTrace);
				return;
			}
			
			//1.X -> 1.3
			if (data.version < 1.3d) {
				// Everything has already been setted to its default value, so let's just adjust the version and save.
				data.version = 1.3d;
				needSave = true;
			}
			
			abandonedButton = new GameObject ("V10Bulldoze abandoned button");
			burnedButton = new GameObject ("V10Bulldoze burned button");
			audioButton = new GameObject ("V10Bulldoze audio button");

			Transform parent = bulldozerBar.transform;
			abandonedButton.transform.parent = parent;
			burnedButton.transform.parent = parent;
			audioButton.transform.parent = parent;
			
			Shader shader = Shader.Find ("UI/Default UI Shader");
			if (shader == null) {
				Debug.Log ("V10Bulldoze: Can't find default UI shader.");
				shader = new Shader ();
				shader.name = "V10Bulldoze dummy shader";
			}
			
			UITextureAtlas atlas = ScriptableObject.CreateInstance<UITextureAtlas> ();
			atlas.name = "V10Bulldoze Atlas";
			atlas.material = new Material (shader);
			atlas.material.mainTexture = new Texture2D (0, 0, TextureFormat.DXT5, false);

			FastList<Texture2D> list = new FastList<Texture2D> ();
			list.EnsureCapacity (18);
			
			UIButton button = abandonedButton.AddComponent<UIButton> ();
			button.relativePosition = new Vector3 (7.0f, -57.0f);
			initButton ("AbandonedButton", button, list, data.abandoned);
			button.atlas = atlas;

			button = burnedButton.AddComponent<UIButton> ();
			button.relativePosition = new Vector3 ((float)(7 + buttonSize + 7), -57.0f);
			initButton ("BurnedButton", button, list, data.burned);
			button.atlas = atlas;
			
			button = audioButton.AddComponent<UIButton> ();
			button.relativePosition = new Vector3 ((float)(7 + buttonSize + 7 + buttonSize + 7), -57.0f);
			initButton ("AudioButton", button, list, !data.disableEffect);
			button.atlas = atlas;
			
			atlas.AddTextures (list.ToArray ());
		}
		
        public static void initButton (string name, UIButton button, FastList<Texture2D> textureList, bool isCheck)
		{
			button.name = name;
			foreach (Texture2D texture in loadTextures (name))
				textureList.Add (texture);
			button.width = button.height = buttonSize;
			setButtonColor (button, isCheck);
			button.eventClick += buttonClick;
        }
		
		private static readonly string[] buttonStates = { "Active", "Inactive" };
		private static readonly string [] buttonModes = { "Normal", "Hovered", "Pressed" };
		private static Texture2D[] loadTextures (string button)
		{
			string prefix = "V10Bulldoze.Assets." + button + ".";
			Stream stream;
			Assembly assembly = Assembly.GetAssembly (typeof(UserInterface));
			BinaryReader reader;
			byte[] bytes;
			
			Texture2D[] textures = new Texture2D[UserInterface.buttonStates.Length * UserInterface.buttonModes.Length];
			int i = 0;
			
			foreach (string state in UserInterface.buttonStates) {
				foreach (string mode in UserInterface.buttonModes) {
					//TODO: try/catch ?
					stream = assembly.GetManifestResourceStream (prefix + state + "." + mode + ".png");
					if (stream == null) {
						Debug.Log ("V10Bulldoze: " + prefix + state + "." + mode + ".png" + " not found!");
						//TODO
					}
					
					reader = new BinaryReader (stream);
					bytes = reader.ReadBytes ((int)stream.Length);
					
					textures [i] = new Texture2D (1, 1, TextureFormat.DXT5, false);
					if (!textures [i].LoadImage (bytes)) {	// If DXT5 fails...
						Debug.Log ("V10Bulldoze: Your system can't encode DXT5. Trying without compression.");
						textures [i] = new Texture2D (1, 1, TextureFormat.ARGB32, false);	// ...try again with RGBA
						if (!textures [i].LoadImage (bytes)) {
							Debug.Log ("V10Bulldoze: Couldn't load " + prefix + state + "." + mode + ".png");
							//TODO
						}
					}
					reader.Close ();
					stream.Close ();
					textures [i].name = "V10Bulldoze" + button + state + mode;
					i++;
				}
			}
			return textures;
		}
		
		private static void setButtonColor (UIButton button, bool active)
		{
			string prefix = "V10Bulldoze" + button.name + (active ? "Active" : "Inactive");
			button.normalFgSprite = button.focusedFgSprite = prefix + "Normal";
			button.hoveredFgSprite = prefix + "Hovered";
			button.pressedFgSprite = prefix + "Pressed";
		}
		
        private static void buttonClick (UIComponent component, UIMouseEventParameter eventParam)
		{
			bool active;
			if (component.gameObject == UserInterface.instance.abandonedButton) {
				UserInterface.instance.data.abandoned = !UserInterface.instance.data.abandoned;
				active = UserInterface.instance.data.abandoned;
			} else if (component.gameObject == UserInterface.instance.burnedButton) {
				UserInterface.instance.data.burned = !UserInterface.instance.data.burned;
				active = UserInterface.instance.data.burned;
			} else {
				UserInterface.instance.data.disableEffect = !UserInterface.instance.data.disableEffect;
				active = !UserInterface.instance.data.disableEffect;
				toggleEffects ();
			}
			UIButton button = (UIButton)component;
			setButtonColor (button, active);
			button.Unfocus ();
			UserInterface.instance.needSave = true;
        }
		
		public static void toggleEffects ()
		{
			SoundEffect effect = null;
			foreach (MultiEffect.SubEffect subEffect in BuildingManager.instance.m_properties.m_bulldozeEffect.GetComponent<MultiEffect> ().m_effects) {
				if (subEffect.m_effect.name == "Building Bulldoze Sound") {
					effect = subEffect.m_effect.GetComponent<SoundEffect> ();
					break;
				}
			}
			if (effect == null) {
				Debug.Log ("V10Bulldoze: Couldn't find AudioClip!");
				//TODO
				return;
			}
			
			float[] toSet;
			if (UserInterface.bulldozeAudioClip == null) {
				float[] tmpData = new float[effect.m_audioInfo.m_clip.samples * effect.m_audioInfo.m_clip.channels];
				effect.m_audioInfo.m_clip.GetData (tmpData, 0);
				UserInterface.bulldozeAudioClip = (float[])tmpData.Clone ();
				toSet = new float[tmpData.Length];
			} else {
				toSet = UserInterface.bulldozeAudioClip;
				UserInterface.bulldozeAudioClip = null;
			}
			effect.m_audioInfo.m_clip.SetData (toSet, 0);
		}
		
		public void destroy ()
		{
			abandonedButton.SetActive (false);
			burnedButton.SetActive (false);
			audioButton.SetActive (false);
			abandonedButton = burnedButton = audioButton = null;
			
			if (needSave) {
				try {
					XmlSerializer serializer = new XmlSerializer (typeof(XmlHolder));
					using (StreamWriter writer = new StreamWriter("V10Bulldoze.xml")) {
						serializer.Serialize (writer, data);
						writer.Flush ();
						writer.Close ();
					}
				} catch (Exception e) {
					Debug.Log ("V10Bulldoze: " + e.GetType ().Name + " while writing xml file: " + e.Message + "\n" + e.StackTrace);
					if (e.InnerException != null) 
						Debug.Log ("Caused by: " + e.InnerException.GetType ().Name + ": " + e.InnerException.Message + "\n" + e.InnerException.StackTrace);
				}
			}
			
			data = null;
		}
    }
	
	[XmlRoot("V10Bulldoze_Configuration")]
	public class XmlHolder
	{
		[XmlElement(ElementName="File_version")]
		public double version = 1.3;
			
    	[XmlElement(ElementName="Demolish_abandoned")]
		public bool abandoned;

    	[XmlElement("Demolish_burned")]
		public bool burned;
		
		[XmlElement("Ignore_Service")] // New since v1.3
		public bool service;
		
		[XmlElement("Interval")]
		public int interval;
		
		[XmlElement("Max_buildings_in_a_row")] // New since v1.1
		public int max;
		
		[XmlElement("Disable_bulldoze_effect")] // New since v1.2
		public bool disableEffect;
		
		public XmlHolder ()
		{
			this.abandoned = this.burned = true;
			this.interval = 10;
			this.max = 256;
			this.disableEffect = this.service = false;
		}
	}
}
