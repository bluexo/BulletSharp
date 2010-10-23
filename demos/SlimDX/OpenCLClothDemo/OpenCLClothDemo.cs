﻿using System;
using System.Drawing;
using System.Windows.Forms;
using BulletSharp;
using DemoFramework;
using SlimDX;
using SlimDX.Direct3D9;

namespace OpenCLClothDemo
{
    class OpenCLClothDemo : Game
    {
        int Width = 1024, Height = 768;
        Vector3 eye = new Vector3(50, 20, 100);
        Vector3 target = new Vector3(0, 20, 40);
        Color ambient = Color.Gray;
        DebugDrawModes debugMode = DebugDrawModes.DrawAabb;

        Light light;
        Material activeMaterial, passiveMaterial, groundMaterial, softBodyMaterial;
        GraphicObjectFactory mesh;
        Physics physics;
        Texture amdFlag;
        Texture atiFlag;

        protected override void OnInitializeDevice()
        {
            Form.ClientSize = new Size(Width, Height);
            Form.Text = "BulletSharp - OpenCL Cloth Demo";

            DeviceSettings9 settings = new DeviceSettings9();
            settings.CreationFlags = CreateFlags.HardwareVertexProcessing;
            settings.Windowed = true;
            settings.MultisampleType = MultisampleType.FourSamples;
            try
            {
                InitializeDevice(settings);
            }
            catch
            {
                // Disable 4xAA if not supported
                settings.MultisampleType = MultisampleType.None;
                InitializeDevice(settings);
            }
        }

        protected override void OnInitialize()
        {
            mesh = new GraphicObjectFactory(Device);

            light = new Light();
            light.Type = LightType.Point;
            light.Range = 140;
            light.Position = new Vector3(10, 30, 50);
            light.Diffuse = Color.LemonChiffon;
            light.Attenuation0 = 1.0f;

            activeMaterial = new Material();
            activeMaterial.Diffuse = Color.Orange;
            activeMaterial.Ambient = ambient;

            passiveMaterial = new Material();
            passiveMaterial.Diffuse = Color.Red;
            passiveMaterial.Ambient = ambient;

            groundMaterial = new Material();
            groundMaterial.Diffuse = Color.Green;
            groundMaterial.Ambient = ambient;

            softBodyMaterial = new Material();
            softBodyMaterial.Diffuse = Color.White;
            softBodyMaterial.Ambient = ambient;

            amdFlag = Texture.FromFile(Device, "amdFlag.png");
            atiFlag = Texture.FromFile(Device, "atiFlag.png");

            Freelook.SetEyeTarget(eye, target);

            Fps.Text = "Move using mouse and WASD+shift\n" +
                "F3 - Toggle debug\n" +
                "F11 - Toggle fullscreen\n" +
                "Space - Shoot box";

            physics = new Physics();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                mesh.Dispose();
                amdFlag.Dispose();
                atiFlag.Dispose();
            }
        }

        protected override void OnResourceLoad()
        {
            base.OnResourceLoad();

            Device.SetLight(0, light);
            Device.EnableLight(0, true);
            Device.SetRenderState(RenderState.Ambient, ambient.ToArgb());

            Projection = Matrix.PerspectiveFovLH(FieldOfView, AspectRatio, 0.1f, 200.0f);
            Device.SetTransform(TransformState.Projection, Projection);

            Device.SetRenderState(RenderState.CullMode, Cull.None);
            Device.SetSamplerState(0, SamplerState.MagFilter, TextureFilter.Linear);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (Input.KeysPressed.Contains(Keys.F3))
            {
                if (physics.IsDebugDrawEnabled == false)
                    physics.SetDebugDrawMode(Device, debugMode);
                else
                    physics.SetDebugDrawMode(Device, DebugDrawModes.None);
            }

            InputUpdate(Freelook.Eye, Freelook.Target, physics);
            physics.Update(FrameDelta);
        }

        protected override void OnRender()
        {
            Device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.LightGray, 1.0f, 0);
            Device.BeginScene();

            Device.SetTransform(TransformState.View, Freelook.View);

            foreach (CollisionObject colObj in physics.World.CollisionObjectArray)
            {
                if (colObj.CollisionShape.ShapeType == BroadphaseNativeType.SoftBodyShape)
                {
                    Device.SetTexture(0, atiFlag);
                    Device.Material = softBodyMaterial;
                    Device.SetTransform(TransformState.World, Matrix.Identity);
                    mesh.RenderSoftBodyTextured(BulletSharp.SoftBody.SoftBody.Upcast(colObj));
                    Device.SetTexture(0, null);
                    continue;
                }
                RigidBody body = RigidBody.Upcast(colObj);
                Device.SetTransform(TransformState.World, body.MotionState.WorldTransform);

                if ((string)colObj.UserObject == "Ground")
                    Device.Material = groundMaterial;
                else if (colObj.ActivationState == ActivationState.ActiveTag)
                    Device.Material = activeMaterial;
                else
                    Device.Material = passiveMaterial;

                mesh.Render(body.CollisionShape, Matrix.Identity);
            }

            physics.DebugDrawWorld();

            Fps.OnRender(FramesPerSecond);

            Device.EndScene();
            Device.Present();
        }

        public Device Device
        {
            get { return Device9; }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            OpenCLClothDemo game = new OpenCLClothDemo();

            if (game.TestLibraries() == false)
                return;

            game.Run();
            game.Dispose();
        }
    }
}
