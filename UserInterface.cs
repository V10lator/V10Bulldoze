using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.IO;
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
		private static readonly Color gray = new Color32(255, 90, 0, 192);
		public static EffectInfo bulldozeEffect = null;
		private static int buttonWidth = 175;

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
			
			//1.X -> 1.2
			if (data.version < 1.2d) {
				// Everything has already been setted to its default value, so let's just adjust the version and save.
				data.version = 1.2d;
				needSave = true;
			}
			
			abandonedButton = new GameObject ("V10Bulldoze abandoned button");
			burnedButton = new GameObject ("V10Bulldoze burned button");
			audioButton = new GameObject ("V10Bulldoze audio button");

			Transform parent = bulldozerBar.transform;
			abandonedButton.transform.parent = parent;
			burnedButton.transform.parent = parent;
			audioButton.transform.parent = parent;

			UIButton button = abandonedButton.AddComponent<UIButton> ();
			button.relativePosition = new Vector3 (7.0f, -7.0f);
			button.text = "Demolish Abandoned";
			initButton (button, data.abandoned);

			float spacer = (float) (7 + buttonWidth);
			
			button = burnedButton.AddComponent<UIButton> ();
			button.relativePosition = new Vector3 (spacer, -7.0f);
			button.text = "Demolish Burned";
			initButton (button, data.burned);
			
			button = audioButton.AddComponent<UIButton> ();
			button.relativePosition = new Vector3 (spacer + spacer, -7.0f);
			button.text = "Play effects";
			initButton (button, !data.disableEffect);
		}
		
        public static void initButton (UIButton button, bool isCheck)
		{
			button.width = buttonWidth;
			button.height = 30;
			string sprite = "SubBarButtonBase";
			string spriteHov = sprite + "Hovered";
			button.normalBgSprite = spriteHov;
			button.disabledBgSprite = spriteHov;
			button.hoveredBgSprite = spriteHov;
			button.focusedBgSprite = spriteHov;
			button.pressedBgSprite = sprite + "Pressed";
			setButtonColor(button, isCheck);
			button.eventClick += buttonClick;
        }
		
		private static void setButtonColor (UIButton button, bool active)
		{
			Color bg;
			Color text;
			if (active) {
				bg = Color.red;
				text = Color.yellow;
			} else {
				bg = UserInterface.gray;
				text = Color.white;
			}
			
			button.color = 
                    	button.focusedColor = 
                    	button.hoveredColor = 
						button.pressedColor = bg;
			button.textColor = text;
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
			if (UserInterface.bulldozeEffect == null) {
				UserInterface.bulldozeEffect = BuildingManager.instance.m_properties.m_bulldozeEffect;
				BuildingManager.instance.m_properties.m_bulldozeEffect = null;
			} else {
				BuildingManager.instance.m_properties.m_bulldozeEffect = UserInterface.bulldozeEffect;
				UserInterface.bulldozeEffect = null;
			}
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
		public double version = 1.2;
			
    	[XmlElement(ElementName="Demolish_abandoned")]
		public bool abandoned;

    	[XmlElement("Demolish_burned")]
		public bool burned;
		
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
			this.disableEffect = false;
		}
	}
}
