using ColossalFramework;
using ICities;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace V10Bulldoze
{
	public class V10Bulldoze : IUserMod
	{
		public static bool active = false;
		public static UserInterface ui = null;
		private string name = null, desc = null;
		
		public string Name {
			get {
				if (this.name == null)
					readAssembly ();
				return this.name;
			}
		}
		
		public string Description {
			get {
				if (this.desc == null)
					readAssembly ();
				return this.desc;
			}
		}
		
		private void readAssembly ()
		{
			Assembly me = Assembly.GetExecutingAssembly ();
			this.name = me.GetName ().Name + " v" + me.GetName ().Version;
			Type type = typeof(CustomAssemblyVariable);
			if (CustomAssemblyVariable.IsDefined (me, type)) {
				string releaseType = ((CustomAssemblyVariable)CustomAssemblyVariable.GetCustomAttribute (
				me,
				type
				)).value;
				if (releaseType != "Release")
					this.name += " - " + releaseType + " version!";
			}
			type = typeof(AssemblyDescriptionAttribute);
			if (AssemblyDescriptionAttribute.IsDefined (me, type))
				this.desc = ((AssemblyDescriptionAttribute)AssemblyDescriptionAttribute.GetCustomAttribute (me, type)).Description;
			else
				this.desc = "N/A";
		}
	}
	
	public class V10BulldozeLoader : LoadingExtensionBase
	{
		public override void OnLevelLoaded (LoadMode mode)
		{
			if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame) {
				SkylinesOverwatch.Settings.Instance.Enable.BuildingMonitor = V10Bulldoze.active = true;
				V10Bulldoze.ui = new UserInterface ();
				if (V10Bulldoze.ui.data.disableEffect)
					UserInterface.toggleEffects ();
			}
		}
		
		public override void OnLevelUnloading ()
		{
			if (!V10Bulldoze.active)
				return;
			V10Bulldoze.active = false;
			if (UserInterface.bulldozeAudioClip != null)
				UserInterface.toggleEffects ();
			V10Bulldoze.ui.destroy ();
			V10Bulldoze.ui = null;
		}
	}
	
	public class V10BulldozeThreader : ThreadingExtensionBase
	{
		private short c = 0, c2 = 0;
		SimulationManager simulationManager = null;
		BulldozeTool bulldozeTool = null;
		MethodInfo method = null;
		
		public override void OnAfterSimulationTick ()
		{
			if (!V10Bulldoze.active || c++ < V10Bulldoze.ui.data.interval)
				return;
			c = 0;
			
			if (!V10Bulldoze.ui.data.abandoned && !V10Bulldoze.ui.data.burned)
				return;
			
			if (simulationManager == null) {
				simulationManager = SimulationManager.instance;
				bulldozeTool = GameObject.FindObjectOfType<BulldozeTool> ();
				method = bulldozeTool.GetType ().GetMethod ("DeleteBuilding", BindingFlags.NonPublic | BindingFlags.Instance);
			}
			
			if (V10Bulldoze.ui.data.abandoned)
				checkBuildings (SkylinesOverwatch.Data.Instance.BuildingsAbandoned);
			if (V10Bulldoze.ui.data.burned && c2 <= V10Bulldoze.ui.data.max) {
				ushort [] toCheck = SkylinesOverwatch.Data.Instance.BuildingsBurnedDown;
				if (V10Bulldoze.ui.data.service)
					toCheck = toCheck.Except (SkylinesOverwatch.Data.Instance.PlayerBuildings).ToArray ();
				checkBuildings (toCheck);
			}
			
			c2 = 0;
		}
		
		private void checkBuildings (ushort[] buildings)
		{
			foreach (ushort toBulldoze in buildings) {
				if (c2++ > V10Bulldoze.ui.data.max)
					break;
				
				simulationManager.AddAction ((IEnumerator) method.Invoke (bulldozeTool, new object[] { toBulldoze }));
				SkylinesOverwatch.Helper.Instance.RequestBuildingRemoval (toBulldoze);
			}
		}
	}
}

