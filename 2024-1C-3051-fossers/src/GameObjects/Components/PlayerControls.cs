using System;
using BepuPhysics.Constraints;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using WarSteel.Common;
using WarSteel.Common.Shaders;
using WarSteel.Entities;
using WarSteel.Managers;
using WarSteel.Utils;

namespace WarSteel.Scenes.Main
{
    public class PlayerControls : IComponent
    {
        private const int ReloadingTimeInMs = 1000;
        private const float ForwardForce = -100000f;
        private const float TorqueForce = 750000f;
        private const float BulletForce = 3600000 * 3;
        private const float BulletMass = 500 * 3;
        private const float FlipTimeThreshold = 10f;
        private const float HealthReductionOnFlip = 1f;
        private const float CanMoveAngleThreshold = 0.5f;
        private const float FlipAngleThreshold = 0.6f;
        private const float BulletPositionOffsetForward = 1000f;
        private const float BulletPositionOffsetUp = 200f;
        private const float TankRotationSpeed = 15f;

        private DynamicBody _rb;
        private bool _isReloading = false;

        private float _startFlipTime = 0;
        private bool _canMove = false;

        private Transform _tankCannon;

        public PlayerControls(Transform tankCannon)
        {
            _tankCannon = tankCannon;
            AudioManager.Instance.AddSoundEffect(Audios.SHOOT, ContentRepoManager.Instance().GetSoundEffect("tank-shot"));
        }

        public void OnStart(GameObject self, Scene scene)
        {
            _rb = self.GetComponent<DynamicBody>();
        }

        public void OnUpdate(GameObject self, GameTime gameTime, Scene scene)
        {
            if (Vector3.Dot(self.Transform.Up, Vector3.Up) < FlipAngleThreshold)
            {
                if (_startFlipTime == 0)
                {
                    _startFlipTime = gameTime.TotalGameTime.Seconds;
                }

                if (gameTime.TotalGameTime.Seconds - _startFlipTime > FlipTimeThreshold)
                {
                    ((Player)self).Health -= HealthReductionOnFlip;
                }
            }
            else
            {
                _startFlipTime = 0;
            }

            ((Player)self).touchingGround = !(self.GetComponent<DynamicBody>().Velocity.Y > 10);
            _canMove = Vector3.Dot(self.Transform.Up, Vector3.Up) >= CanMoveAngleThreshold && ((Player)self).touchingGround;

            bool isMoving = self.GetComponent<DynamicBody>().Velocity.Length() > 0;

            bool isRotatingL = false;
            bool isRotatingR = false;

            if (Keyboard.GetState().IsKeyDown(Keys.W) && _canMove)
            {
                _rb.ApplyForce(self.Transform.Forward * ForwardForce);
            }
            if (Keyboard.GetState().IsKeyDown(Keys.S) && _canMove)
            {
                _rb.ApplyForce(self.Transform.Backward * ForwardForce);
            }
            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                isRotatingL = true;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                isRotatingR = true;
            }

            float rotationSpeed;

            if (!(isRotatingL || isRotatingR) || isRotatingL && isRotatingR)
            {
                rotationSpeed = 0;
            }
            else if (isRotatingL)
            {
                rotationSpeed = TankRotationSpeed;
            }
            else if (isRotatingR)
            {
                rotationSpeed = -TankRotationSpeed;
            }
            else
            {
                rotationSpeed = 0;
            }


            if (isMoving)
            {
                _rb.ApplyTorque(Vector3.Normalize(self.Transform.Up) * rotationSpeed * TorqueForce);
            }

            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                Shoot(self, scene);
            }


        }

        public void Shoot(GameObject self, Scene scene)
        {
            if (_isReloading) return;


            var bullet = CreateBullet((Player)self, scene);
            AudioManager.Instance.PlaySound(Audios.SHOOT);
            scene.AddGameObject(bullet);
            bullet.GetComponent<DynamicBody>().ApplyForce(-_tankCannon.Forward * BulletForce);
            PlayerEvents.TriggerReload(ReloadingTimeInMs);
            _isReloading = true;
            Timer.Timeout(ReloadingTimeInMs, () => _isReloading = false);
        }

        public GameObject CreateBullet(Player self, Scene scene)
        {
            var bullet = new GameObject(new[] { "bullet" }, new Transform(), ContentRepoManager.Instance().GetModel("Tanks/Bullet"), new Renderer(Color.Red))
            {
                AlwaysRender = true
            };
            bullet.AddComponent(new DynamicBody(new Collider(new SphereShape(10), c =>
        {
            if (c.Entity.HasTag("enemy") && !bullet.HasTag("HitGround") && !bullet.HasTag("HitEnemy"))
            {
                var enemy = (Enemy)c.Entity;
                enemy.Health -= self.Damage;
                enemy.Model.AddImpact(bullet.Transform.AbsolutePosition, bullet.GetComponent<DynamicBody>().Velocity);
                bullet.AddTag("HitEnemy");
            }
            bullet.GetComponent<LightComponent>().DecayFactor = 5f;
            bullet.AddTag("HitGround");
            Timer.Timeout(3000, () => bullet.Destroy());

        }), Vector3.Zero, BulletMass, 0, 0));
            Timer.Timeout(5000, () => bullet.Destroy());
            bullet.AddComponent(new LightComponent(Color.LightSkyBlue,1));
            bullet.GetComponent<DynamicBody>().Velocity = self.GetComponent<DynamicBody>().Velocity;
            bullet.Transform.Position = _tankCannon.AbsolutePosition - _tankCannon.Forward * BulletPositionOffsetForward + _tankCannon.Up * BulletPositionOffsetUp;
            return bullet;
        }



        public void Destroy(GameObject self, Scene scene) { }
    }
}
