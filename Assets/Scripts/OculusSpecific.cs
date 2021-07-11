// Copyright 2021 The Open Brush Authors
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

using UnityEngine;
using Node = UnityEngine.XR.XRNode;
using NodeState = UnityEngine.XR.XRNodeState;

// This requires OVROverlay from Oculus Integration on the Unity Asset Store.

#if OCULUS_SUPPORTED
namespace TiltBrush
{
    public class OculusOverlay : OverlayImplementation
    {
        private OVROverlay m_OVROverlay;
        private GameObject m_VrSystem;

        public override bool Enabled
        {
            get => m_OVROverlay.enabled;
            set => m_OVROverlay.enabled = value;
        }

        public OculusOverlay(GameObject vrsys)
        {
            m_VrSystem = vrsys;
        }

        public override void Initialise()
        {
            var gobj = new GameObject("Oculus Overlay");
            gobj.transform.SetParent(m_VrSystem.transform, worldPositionStays: false);
            m_OVROverlay = gobj.AddComponent<OVROverlay>();
            m_OVROverlay.isDynamic = true;
            m_OVROverlay.compositionDepth = 0;
            m_OVROverlay.currentOverlayType = OVROverlay.OverlayType.Overlay;
            m_OVROverlay.currentOverlayShape = OVROverlay.OverlayShape.Quad;
            m_OVROverlay.noDepthBufferTesting = true;
            m_OVROverlay.enabled = false;
        }

        public override void SetTexture(Texture tex)
        {
            m_OVROverlay.textures = new[] { tex };
        }

        public override void SetAlpha(float ratio)
        {
            Enabled = ratio == 1.0f;
        }

        public override void SetPosition(float distance, float height)
        {
            // place overlay in front of the player a distance out
            Vector3 vOverlayPosition = ViewpointScript.Head.position;
            Vector3 vOverlayDirection = ViewpointScript.Head.forward;
            vOverlayDirection.y = 0.0f;
            vOverlayDirection.Normalize();

            vOverlayPosition += (vOverlayDirection * distance / 10);
            m_OVROverlay.transform.position = vOverlayPosition;
            m_OVROverlay.transform.forward = vOverlayDirection;
        }
        
        public override void PauseRendering(bool bPause)
        {
            // :(
        }

        protected override void FadeToCompositor(float fadeTime, bool fadeToCompositor)
        {
            FadeBlack(fadeTime, fadeToCompositor);
        }
                

        protected override void FadeBlack(float fadeTime, bool fadeToBlack)
        {
            // TODO: using Viewpoint here is pretty gross, dependencies should not go from VrSdk
            // to other Tilt Brush components.

            // Currently ViewpointScript.FadeToColor takes 1/time as a parameter, which we should fix to
            // make consistent, but for now just convert the incoming parameter.
            float speed = 1 / Mathf.Max(fadeTime, 0.00001f);
            if (fadeToBlack)
            {
                ViewpointScript.m_Instance.FadeToColor(Color.black, speed);
            }
            else
            {
                ViewpointScript.m_Instance.FadeToScene(speed);
            }
        }
    }

    public class OculusMRCCameraUpdate : MonoBehaviour
    {
        private OVRPose? calibratedCameraPose = null;

        void Update()
        {
            if (!calibratedCameraPose.HasValue)
            {
                if (!OVRPlugin.Media.GetInitialized())
                {
                    return;
                }

                OVRPlugin.CameraIntrinsics cameraIntrinsics;
                OVRPlugin.CameraExtrinsics cameraExtrinsics;

                if (OVRPlugin.GetMixedRealityCameraInfo(0, out cameraExtrinsics, out cameraIntrinsics))
                {
                    calibratedCameraPose = cameraExtrinsics.RelativePose.ToOVRPose();
                }
                else
                {
                    return;
                }
            }

            OVRPose cameraStagePoseInUnits = calibratedCameraPose.Value;

            // Converting position from meters to decimeters (unit used by Open Brush)
            cameraStagePoseInUnits.position *= App.METERS_TO_UNITS;

            // Workaround to fix the OVRExtensions.ToWorldSpacePose() and 
            // OVRComposition.ComputeCameraWorldSpacePose() calls when computing 
            // the Mixed Reality foreground and background camera positions.
            OVRPose headPose = OVRPose.identity;

            Vector3 pos;
            Quaternion rot;

            if (OVRNodeStateProperties.GetNodeStatePropertyVector3(Node.Head,
                NodeStatePropertyType.Position, OVRPlugin.Node.Head, OVRPlugin.Step.Render, out pos))
            {
                headPose.position = pos;
            }

            if (OVRNodeStateProperties.GetNodeStatePropertyQuaternion(Node.Head,
                NodeStatePropertyType.Orientation, OVRPlugin.Node.Head, OVRPlugin.Step.Render, out rot))
            {
                headPose.orientation = rot;
            }

            OVRPose headPoseInUnits = OVRPose.identity;
            headPoseInUnits.position = headPose.position * App.METERS_TO_UNITS;
            headPoseInUnits.orientation = headPose.orientation;

            OVRPose stageToLocalPose = OVRPlugin.GetTrackingTransformRelativePose(
                OVRPlugin.TrackingOrigin.Stage).ToOVRPose();

            OVRPose stageToLocalPoseInUnits = OVRPose.identity;
            stageToLocalPoseInUnits.position = stageToLocalPose.position * App.METERS_TO_UNITS;
            stageToLocalPoseInUnits.orientation = stageToLocalPose.orientation;

            OVRPose cameraWorldPoseInUnits = headPoseInUnits.Inverse() * stageToLocalPoseInUnits *
                cameraStagePoseInUnits;
            OVRPose cameraStagePoseFix = stageToLocalPose.Inverse() * headPose * cameraWorldPoseInUnits;

            // Override the MRC camera's stage pose
            OVRPlugin.OverrideExternalCameraStaticPose(0, true, cameraStagePoseFix.ToPosef());
        }
    }
} // namespace TiltBrush

#endif // OCULUS_SUPPORTED
