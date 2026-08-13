using MMOTableGame.CameraSystem;
using NUnit.Framework;
using UnityEngine;

namespace MMOTableGame.Tests
{
    public sealed class IsometricCameraFollowTests
    {
        private GameObject cameraObject;
        private IsometricCameraFollow cameraFollow;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("Test Camera");
            cameraObject.AddComponent<Camera>();
            cameraFollow = cameraObject.AddComponent<IsometricCameraFollow>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void PointerAtRightEdgePansCameraToScreenRight()
        {
            cameraFollow.TickEdgePan(new Vector2(799f, 300f), new Vector2(800f, 600f), 1f);

            Assert.That(cameraFollow.PanOffset.sqrMagnitude, Is.GreaterThan(0f));
            Assert.That(Vector3.Dot(cameraFollow.PanOffset, Quaternion.Euler(0f, 45f, 0f) * Vector3.right),
                Is.GreaterThan(0f));
        }

        [Test]
        public void PointerInCenterOrOutsideViewportDoesNotPan()
        {
            cameraFollow.TickEdgePan(new Vector2(400f, 300f), new Vector2(800f, 600f), 1f);
            cameraFollow.TickEdgePan(new Vector2(-1f, 300f), new Vector2(800f, 600f), 1f);

            Assert.That(cameraFollow.PanOffset, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ResetPanOffsetRecentersCameraFocus()
        {
            cameraFollow.Pan(Vector2.up, 1f);
            Assert.That(cameraFollow.PanOffset.sqrMagnitude, Is.GreaterThan(0f));

            cameraFollow.ResetPanOffset();

            Assert.That(cameraFollow.PanOffset, Is.EqualTo(Vector3.zero));
        }
    }
}
