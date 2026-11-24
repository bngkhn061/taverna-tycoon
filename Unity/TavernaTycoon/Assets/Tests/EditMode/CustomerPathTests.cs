using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Taverna.Tests
{
    /// <summary>
    /// v2.5.1 (oyuncu testi 2026-11-24: "müşteriler ilk fıçıya doğru gidiyor sonra masaya oturuyor"): müşteri yolu kapıdan sonra
    /// kestirilir (<see cref="CustomerSpawner.Shortcut"/>). Önü açık oturağa doğrudan gidilir, toplanma noktasına (bira fıçısının
    /// önü) uğranmaz; engel varsa toplanma noktası kalır; hedef oturağın kendi masası engel sayılmaz.
    /// </summary>
    public class CustomerPathTests
    {
        static readonly Vector3 Street = new(0f, 0f, -8f);
        static readonly Vector3 Door = new(0f, 0f, -5.45f);
        static readonly Vector3 Hub = new(0.6f, 0f, 1.2f);
        static readonly Vector3 Approach = new(-0.2f, 0f, 0.25f);
        static readonly Vector3 SeatPos = new(-0.2f, 0f, -0.55f);

        readonly List<GameObject> _made = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var g in _made) if (g) Object.DestroyImmediate(g);
            _made.Clear();
            Physics.SyncTransforms();
        }

        GameObject Block(string name, Vector3 center, Vector3 size, Transform parent = null)
        {
            var g = new GameObject(name, typeof(BoxCollider));
            if (parent) g.transform.SetParent(parent, false); else _made.Add(g);
            g.transform.position = center;
            g.GetComponent<BoxCollider>().size = size;
            return g;
        }

        static List<Vector3> FullPath() => new() { Street, Door, Hub, Approach, SeatPos };

        [Test]
        public void OpenFloor_WalksStraightToSeatSkippingHub()
        {
            // Arrange
            Physics.SyncTransforms();

            // Act
            var path = CustomerSpawner.Shortcut(FullPath(), 1, null);

            // Assert
            CollectionAssert.AreEqual(new[] { Street, Door, SeatPos }, path);
        }

        [Test]
        public void TableBetweenDoorAndSeat_DetoursBesideItNotViaHub()
        {
            // Arrange: kapıyla oturak arasında bir masa (x −1.8..−0.1)
            Block("Engel", new Vector3(-0.95f, 0.5f, -3f), new Vector3(1.7f, 1f, 0.5f));
            Physics.SyncTransforms();

            // Act
            var path = CustomerSpawner.Shortcut(FullPath(), 1, null);

            // Assert: kapı → masanın yanı → oturak; toplanma noktası yok, her parça açık
            Assert.AreEqual(4, path.Count);
            CollectionAssert.DoesNotContain(path, Hub);
            Assert.AreEqual(SeatPos, path[^1]);
            for (int k = 1; k < path.Count - 1; k++)
                Assert.IsTrue(CustomerSpawner.Clear(path[k], path[k + 1], null), $"parça {k} kapalı");
        }

        [Test]
        public void WalledOffExceptHub_KeepsHub()
        {
            // Arrange: kapının önü, toplanma noktasına giden dar aralık dışında baştan başa kapalı
            Block("SolDuvar", new Vector3(-3.075f, 0.5f, -3f), new Vector3(6.05f, 1f, 0.3f));   // x −6.1..−0.05
            Block("SagDuvar", new Vector3(3.55f, 0.5f, -3f), new Vector3(6.0f, 1f, 0.3f));      // x +0.55..+6.55
            Block("Arka", new Vector3(-0.425f, 0.5f, -1.4f), new Vector3(0.95f, 1f, 0.3f));     // oturağın önü, x −0.9..+0.05
            Physics.SyncTransforms();
            Assume.That(CustomerSpawner.Clear(Door, Hub, null), "toplanma noktasına giden aralık açık olmalı");

            // Act
            var path = CustomerSpawner.Shortcut(FullPath(), 1, null);

            // Assert
            Assert.AreEqual(Hub, path[2]);
            Assert.AreEqual(SeatPos, path[^1]);
        }

        [Test]
        public void OwnTableAtSeat_DoesNotBlockLastStep()
        {
            // Arrange: oturağın kendi masası oturağa değiyor; başka masa olsa engel olurdu
            var root = new GameObject("Masa");
            _made.Add(root);
            var table = root.AddComponent<Table>();
            Block("Model", SeatPos + new Vector3(0.45f, 0.4f, 0f), new Vector3(0.6f, 0.8f, 0.6f), root.transform);
            var seatGo = new GameObject("Seat0");
            seatGo.transform.SetParent(root.transform, false);
            seatGo.transform.position = SeatPos;
            var seat = seatGo.AddComponent<Seat>();
            seat.table = table;
            Physics.SyncTransforms();

            var west = new Vector3(-1.5f, 0f, -0.55f);   // oturağın açık yanı
            var east = new Vector3(1.5f, 0f, -0.55f);    // masanın öbür yanı: arada kendi masası var

            // Act
            bool fromOpenSide = CustomerSpawner.Clear(west, SeatPos, seat);
            bool forOtherSeat = CustomerSpawner.Clear(west, SeatPos, null);
            bool throughOwnTable = CustomerSpawner.Clear(east, SeatPos, seat);

            // Assert
            Assert.IsTrue(fromOpenSide, "kendi masası son adımı kesmez");
            Assert.IsFalse(forOtherSeat, "başka bir oturağın gözünden masa engeldir");
            Assert.IsFalse(throughOwnTable, "müşteri kendi masasının içinden geçmez");
        }

        [Test]
        public void StairsBetweenFloors_NeverShortcut()
        {
            // Arrange: biri üst katta (teras dünyada ayrı yerde, z > Inn.UpZ)
            var up = new Vector3(0f, 0f, 40f);

            // Act
            bool clear = CustomerSpawner.Clear(Door, up, null);

            // Assert
            Assert.IsFalse(clear);
        }
    }
}
