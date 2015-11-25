using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code derived from FSengineHover by Snjo
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
    public class WBIMultiEngineHover : PartModule
    {
        [KSPField]
        public float verticalSpeedIncrements = 1f;

        [KSPField(guiActive = true, guiName = "Vertical Speed")]
        public float verticalSpeed = 0f;

        [KSPField(isPersistant = true)]
        public bool hoverActive = false;

        [KSPField]
        public float thrustSmooth = 0.01f;

        [KSPField(isPersistant = true)]
        public float maxThrust = 0f;

        [KSPField(isPersistant = true)]
        public bool maxThrustFetched = false;

        [KSPField]
        public bool useHardCodedButtons = true;

        [KSPField(isPersistant = true)]
        private bool runningPrimary;

        private ModuleEngines primaryEngine;
        private ModuleEngines secondaryEngine;
        private ModuleEngines currentEngine;
        private MultiModeEngine multiModeEngine;
        private float currentThrustNormalized = 0f;
        private float targetThrustNormalized = 0f;
        private float minThrust = 0f;
        private bool guiVisible = false;

        [KSPEvent(guiActive = true, guiName = "Toggle Hover")]
        public void ToggleHoverMode()
        {
            hoverActive = !hoverActive;
            if (hoverActive)
                ActivateHover();
            else
                DeactivateHover();
        }

        /*
        [KSPEvent(guiActive = true, guiName = "Test")]
        public void Test()
        {
            FlightGlobals.ActiveVessel.ctrlState.mainThrottle = 0.3f;
            FlightInputHandler.state.mainThrottle = 0.3f;
        }
         */

        [KSPEvent(guiActive = true, guiName = "Vertical Speed +")]
        public void IncreaseVSpeed()
        {
            verticalSpeed += verticalSpeedIncrements;
            printSpeed();
        }

        [KSPEvent(guiActive = true, guiName = "Vertical Speed -")]
        public void DecreaseVSpeed()
        {
            verticalSpeed -= verticalSpeedIncrements;
            printSpeed();
        }

        [KSPAction("Toggle Hover")]
        public void toggleHoverAction(KSPActionParam param)
        {
            ToggleHoverMode();
        }

        [KSPAction("Vertical Speed +")]
        public void increaseVerticalSpeed(KSPActionParam param)
        {
            IncreaseVSpeed();
        }

        [KSPAction("Vertical Speed -")]
        public void decreaseVerticalSpeed(KSPActionParam param)
        {
            DecreaseVSpeed();
        }

        public void KillVSpeed()
        {
            verticalSpeed = 0f;
            printSpeed();
        }

        [KSPEvent(guiActive = true, guiName = "Show GUI")]
        public void ShowGUI()
        {
            WBIHoverManager.Instance.ShowGUI();
        }

        public void SetGUIVisible(bool isVisible)
        {
            guiVisible = isVisible;
            Events["ToggleHoverMode"].guiActive = isVisible;
            Events["IncreaseVSpeed"].guiActive = isVisible;
            Events["DecreaseVSpeed"].guiActive = isVisible;
            Fields["verticalSpeed"].guiActive = isVisible;

            //ShowGUI event is always shown...
        }

        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight)
            {
                multiModeEngine = this.part.FindModuleImplementing<MultiModeEngine>();
                if (multiModeEngine == null)
                {
                    currentEngine = this.part.FindModuleImplementing<ModuleEngines>();
                }

                else //Find the primary and secondary engines
                {
                    List<ModuleEngines> engineList = this.part.FindModulesImplementing<ModuleEngines>();
                    foreach (ModuleEngines engine in engineList)
                    {
                        if (engine.engineID == multiModeEngine.primaryEngineID)
                            primaryEngine = engine;
                        else if (engine.engineID == multiModeEngine.secondaryEngineID)
                            secondaryEngine = engine;
                    }

                    //Now set the current engine
                    SetCurrentEngine();
                }

                //Set min/max thrust
                SetMinMaxThrust();

                //Set hover state
                if (hoverActive)
                    ActivateHover();
                else
                    DeactivateHover();

                //Set gui visible state
                SetGUIVisible(guiVisible);
            }
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (HighLogic.LoadedSceneIsFlight && vessel == FlightGlobals.ActiveVessel)
            {
                if (hoverActive)
                {
                    //Do we go up or down?
                    if (vessel.verticalSpeed >= verticalSpeed)
                        targetThrustNormalized = 0f;
                    else if (vessel.verticalSpeed < verticalSpeed)
                        targetThrustNormalized = 1f;

                    //Normalize the thrust
                    currentThrustNormalized = Mathf.Lerp(currentThrustNormalized, targetThrustNormalized, thrustSmooth);

                    //Calculate new thrust
                    float newThrust = maxThrust * currentThrustNormalized;
                    if (newThrust <= minThrust) 
                        newThrust = minThrust + 0.001f;

                    //Set the throttle based upon thrust
                    if (currentEngine != null)
                    {
                        currentEngine.currentThrottle = newThrust / maxThrust;
                    }
                    else
                    {
                        Debug.Log("currentEngine is null");
                    }
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (HighLogic.LoadedSceneIsFlight && vessel == FlightGlobals.ActiveVessel)
            {
                //Make sure we have the right engine
                if (multiModeEngine != null)
                {
                    if (runningPrimary != multiModeEngine.runningPrimary)
                    {
                        SetCurrentEngine();
                        SetMinMaxThrust();
                    }
                }

                /*
                //Hover controls
                if (useHardCodedButtons)
                {
                    if (Input.GetKeyDown(KeyCode.PageUp))
                    {
                        verticalSpeed += verticalSpeedIncrements;
                        printSpeed();
                    }
                    if (Input.GetKeyDown(KeyCode.PageDown))
                    {
                        verticalSpeed -= verticalSpeedIncrements;
                        printSpeed();
                    }
                    if (Input.GetKeyDown(KeyCode.Delete))
                    {
                        verticalSpeed = 0f;
                        printSpeed();
                    }
                    if (Input.GetKeyDown(KeyCode.Insert))
                    {
                        ToggleHoverMode();
                    }
                }
                 */

                //Monitor the throttle
                if (FlightInputHandler.state.mainThrottle <= 0.001f && hoverActive)
                    DeactivateHover();
            }
        }

        #region Helpers
        public void printSpeed()
        {
            ScreenMessages.PostScreenMessage(new ScreenMessage("Hover Climb Rate: " + verticalSpeed, 1f, ScreenMessageStyle.UPPER_CENTER));
        }

        public void SetCurrentEngine()
        {
            if (multiModeEngine == null)
                return;

            if (multiModeEngine.runningPrimary)
            {
                runningPrimary = true;
                currentEngine = primaryEngine;
            }
            else
            {
                runningPrimary = false;
                currentEngine = secondaryEngine;
            }
        }

        public void SetMinMaxThrust()
        {
            if (currentEngine != null)
            {
                if (maxThrustFetched && maxThrust > 0f)
                {
                    currentEngine.maxThrust = maxThrust;
                }
                else
                {
                    maxThrust = currentEngine.maxThrust;
                    maxThrustFetched = true;
                }
                minThrust = currentEngine.minThrust;
            }
        }

        public void ActivateHover()
        {
            if (currentEngine != null)
            {
                hoverActive = true;
                verticalSpeed = 0f;
                vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                if (guiVisible)
                {
                    Events["ToggleHoverMode"].guiName = "Turn Off Hover Mode";
                    Events["IncreaseVSpeed"].guiActive = true;
                    Events["DecreaseVSpeed"].guiActive = true;
                    Fields["verticalSpeed"].guiActive = true;
                }
                ScreenMessages.PostScreenMessage(new ScreenMessage("Hover Mode On", 1f, ScreenMessageStyle.UPPER_CENTER));
            }
        }

        public void DeactivateHover()
        {
            if (currentEngine != null)
            {
                currentEngine.currentThrottle = FlightInputHandler.state.mainThrottle;
                hoverActive = false;
                verticalSpeed = 0f;
                vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, false);
                if (guiVisible)
                {
                    Events["ToggleHoverMode"].guiName = "Turn On Hover Mode";
                    Events["IncreaseVSpeed"].guiActive = false;
                    Events["DecreaseVSpeed"].guiActive = false;
                    Fields["verticalSpeed"].guiActive = false;
                }
                ScreenMessages.PostScreenMessage(new ScreenMessage("Hover Mode Off", 1f, ScreenMessageStyle.UPPER_CENTER));
            }
        }
        #endregion
    }
}