using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyrighgt 2015, by Michael Billard (Angel-125)
License: CC BY-NC-SA 4.0
License URL: https://creativecommons.org/licenses/by-nc-sa/4.0/
If you want to use this code, give me a shout on the KSP forums! :)
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class WBIHoverManager : MonoBehaviour
    {
        public static WBIHoverManager Instance;

        public KeyCode codeToggleHover = KeyCode.Insert;
        public KeyCode codeIncreaseVSpeed = KeyCode.PageUp;
        public KeyCode codeDecreaseVSpeed = KeyCode.PageDown;
        public KeyCode codeZeroVSpeed = KeyCode.Delete;

        public bool hoverActive = false;
        public Vessel vessel;

        private List<WBIMultiEngineHover> hoverEngines = new List<WBIMultiEngineHover>();
        private HoverVTOLGUI hoverGUI;

        public void Start()
        {
            WBIHoverManager.Instance = this;
            GameEvents.onVesselLoaded.Add(VesselWasLoaded);
            GameEvents.onVesselChange.Add(VesselWasChanged);

            hoverGUI = new HoverVTOLGUI();
            hoverGUI.hoverManager = this;
        }

        public void VesselWasChanged(Vessel vessel)
        {
            FindHoverEngines(vessel);
        }

        public void VesselWasLoaded(Vessel vessel)
        {
            FindHoverEngines(vessel);
        }

        public void FindHoverEngines(Vessel vessel)
        {
            WBIMultiEngineHover hoverEngine;

            hoverEngines.Clear();

            foreach (Part part in vessel.parts)
            {
                hoverEngine = part.FindModuleImplementing<WBIMultiEngineHover>();

                if (hoverEngine != null)
                    hoverEngines.Add(hoverEngine);
            }

            this.vessel = vessel;
        }

        public void DecreaseVSpeed()
        {
            if (hoverActive == false)
                ToggleHover();

            foreach (WBIMultiEngineHover hoverEngine in hoverEngines)
                hoverEngine.DecreaseVSpeed();
        }

        public void IncreaseVSpeed()
        {
            if (hoverActive == false)
                ToggleHover();

            foreach (WBIMultiEngineHover hoverEngine in hoverEngines)
                hoverEngine.IncreaseVSpeed();
        }

        public void KillVSpeed()
        {
            if (hoverActive == false)
                ToggleHover();

            foreach (WBIMultiEngineHover hoverEngine in hoverEngines)
                hoverEngine.KillVSpeed();
        }

        public void ToggleHover()
        {
            hoverActive = !hoverActive;

            foreach (WBIMultiEngineHover hoverEngine in hoverEngines)
                hoverEngine.ToggleHoverMode();
        }

        public void ShowGUI()
        {
            hoverGUI.SetVisible(true);
        }

        public void Update()
        {
            if (Input.GetKeyDown(codeDecreaseVSpeed))
                DecreaseVSpeed();

            if (Input.GetKeyDown(codeIncreaseVSpeed))
                IncreaseVSpeed();

            if (Input.GetKeyDown(codeZeroVSpeed))
                KillVSpeed();

            if (Input.GetKeyDown(codeToggleHover))
                ToggleHover();
        }
    }
}
