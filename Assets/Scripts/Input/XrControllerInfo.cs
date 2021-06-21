// Copyright 2020 The Tilt Brush Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace TiltBrush
{
    public class XrControllerInfo : ControllerInfo
    {
        private InputDeviceCharacteristics m_characteristics;
        private InputDevice m_device;

        public override bool IsTrackedObjectValid { get; set; }

        public XrControllerInfo(BaseControllerBehavior behavior, bool isLeftHand) : base(behavior)
        {
            InputDevices.deviceConnected += OnDeviceConnected;

            m_characteristics = InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.TrackedDevice;

            m_characteristics |= isLeftHand ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right;
        }

        ~XrControllerInfo()
        {
            InputDevices.deviceConnected -= OnDeviceConnected;
        }

        private void OnDeviceConnected(InputDevice device)
        {
            if (!m_device.isValid && (device.characteristics & m_characteristics) == m_characteristics)
            {
                m_device = device;
            }
        }

        // Update device 
        public void UpdatePoses()
        {
            IsTrackedObjectValid = m_device.isValid;

            if (m_device.isValid)
            {
                Transform t = Behavior.transform;

                Vector3 position;
                if (m_device.TryGetFeatureValue(CommonUsages.devicePosition, out position))
                {
                    t.localPosition = position;
                }

                Quaternion rotation;
                if (m_device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
                {
                    t.localRotation = rotation;
                }
            }
        }

        public override void Update()
        {
            base.Update();
            UpdatePoses();
        }

        public override float GetTriggerRatio()
        {
            float value;
            if (m_device.isValid && m_device.TryGetFeatureValue(CommonUsages.trigger, out value))
            {
                return value;
            }

            return 0;
        }

        public override Vector2 GetPadValue()
        {
            Vector2 value;
            if (m_device.isValid && m_device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out value))
            {
                return value;
            }

            return Vector2.zero;
        }

        public override Vector2 GetThumbStickValue()
        {
            Vector2 value;
            if (m_device.isValid && m_device.TryGetFeatureValue(CommonUsages.primary2DAxis, out value))
            {
                return value;
            }

            return Vector2.zero;
        }

        public override Vector2 GetPadValueDelta()
        {
            return Vector2.zero;
        }

        public override float GetGripValue()
        {
            float value;
            if (m_device.isValid && m_device.TryGetFeatureValue(CommonUsages.grip, out value))
            {
                return value;
            }

            return 0.0f;
        }

        public override float GetTriggerValue()
        {
            float value;
            if (m_device.isValid && m_device.TryGetFeatureValue(CommonUsages.trigger, out value))
            {
                return value;
            }

            return 0.0f;
        }

        public override float GetScrollXDelta()
        {
            if (IsTrackedObjectValid)
            {
                return GetThumbStickValue().x;
            }

            return 0.0f;
        }

        public override float GetScrollYDelta()
        {
            if (IsTrackedObjectValid)
            {
                return GetThumbStickValue().y;
            }

            return 0.0f;
        }

        private bool CheckVrTouch(VrInput input)
        {
            bool value = false;
            switch (input)
            {
                case VrInput.Button01:
                case VrInput.Button04:
                case VrInput.Button06:
                    if (m_device.TryGetFeatureValue(CommonUsages.primaryButton, out value))
                    {
                        return value;
                    }
                    break;
                case VrInput.Button02:
                case VrInput.Button03:
                case VrInput.Button05:
                    if (m_device.TryGetFeatureValue(CommonUsages.secondaryButton, out value))
                    {
                        return value;
                    }
                    break;
                case VrInput.Directional:
                case VrInput.Thumbstick:
                case VrInput.Touchpad:
                    if (m_device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out value))
                    {
                        return value;
                    }
                    break;
                case VrInput.Any:
                    // return  OVRInput.Touch.One |
                    //         OVRInput.Touch.Two |
                    //         OVRInput.Touch.PrimaryThumbstick;
                    if (m_device.TryGetFeatureValue(CommonUsages.primaryButton, out value) && value)
                        return true;
                    if (m_device.TryGetFeatureValue(CommonUsages.secondaryButton, out value) && value)
                        return true;
                    if (m_device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out value) && value)
                        return true;
                    break;
                default:
                    Debug.Assert(false, $"Invalid touch button enum: {input.ToString()}");
                    break;
            }

            return value;
        }

        public override bool GetVrInputTouch(VrInput input)
        {
            if (m_device.isValid)
            {
                return CheckVrTouch(input);
            }

            return false;
        }

        private bool CheckVrInput(VrInput input)
        {
            bool value = false;
            switch (input)
            {
                case VrInput.Directional:
                case VrInput.Thumbstick:
                case VrInput.Touchpad:
                    if (m_device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out value))
                    {
                        return value;
                    }
                    break;

                case VrInput.Trigger:
                    if (m_device.TryGetFeatureValue(CommonUsages.triggerButton, out value))
                    {
                        return value;
                    }
                    break;

                case VrInput.Grip:
                    if (m_device.TryGetFeatureValue(CommonUsages.gripButton, out value))
                    {
                        return value;
                    }
                    break;

                case VrInput.Button04:
                case VrInput.Button01:
                case VrInput.Button06:
                    // Pad_Left, Pad_Down, Full pad, (X,A)
                    if (m_device.TryGetFeatureValue(CommonUsages.primaryButton, out value))
                    {
                        return value;
                    }
                    break;

                case VrInput.Button02:
                case VrInput.Button03:
                case VrInput.Button05:
                    // Pad_Right, Pad_Up, Application button, (Y,B)
                    if (m_device.TryGetFeatureValue(CommonUsages.secondaryButton, out value))
                    {
                        return value;
                    }
                    break;

                case VrInput.Any:
                    //     return OVRInput.Button.One
                    //         | OVRInput.Button.Two
                    //         | OVRInput.Button.PrimaryThumbstick
                    //         | OVRInput.Button.PrimaryIndexTrigger
                    //         | OVRInput.Button.PrimaryHandTrigger
                    if (m_device.TryGetFeatureValue(CommonUsages.primaryButton, out value) && value)
                        return true;
                    if (m_device.TryGetFeatureValue(CommonUsages.secondaryButton, out value) && value)
                        return true;
                    if (m_device.TryGetFeatureValue(CommonUsages.triggerButton, out value) && value)
                        return true;
                    if (m_device.TryGetFeatureValue(CommonUsages.gripButton, out value) && value)
                        return true;
                    if (m_device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out value) && value)
                        return true;
                    break;
            }

            return value; // Should never get here.
        }

        public override bool GetVrInput(VrInput input)
        {
            if (m_device.isValid)
            {
                return CheckVrInput(input);
            }

            return false;
        }

        public override bool GetVrInputDown(VrInput input)
        {
            if (m_device.isValid)
            {
                return CheckVrInput(input);
            }

            return false;
        }

        public override void TriggerControllerHaptics(float seconds)
        {
        }
    }
}
