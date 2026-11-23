using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Taverna.Tests
{
    /// <summary>
    /// v2.5 cila (GAME_DESIGN.md §19): son sahnede kameranın önüne giren büyük nesneler (tabela, sancak, sütun) gizlenir; konunun
    /// yanında ya da arkasında duran ve alçak nesneler gizlenmez; sahne bitince hepsi geri gelir (<see cref="CinemaOccluders"/>).
    /// </summary>
    public class V25Tests
    {
        readonly List<GameObject> _made = new();
        Camera _cam;
        static readonly Vector3 Subject = new(0f, 0.8f, 0f);

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("V25_Cam");
            _made.Add(go);
            _cam = go.AddComponent<Camera>();
            _cam.fieldOfView = 50f;
            _cam.aspect = 1266f / 585f;
            go.transform.position = new Vector3(0f, 4f, -8f);
            go.transform.LookAt(Subject);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var g in _made) if (g) Object.DestroyImmediate(g);
            _made.Clear();
        }

        Renderer Box(string name, Vector3 center, Vector3 size)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.position = center;
            g.transform.localScale = size;
            _made.Add(g);
            return g.GetComponent<Renderer>();
        }

        void Check(CinemaOccluders o) => o.Check(_cam, _cam.transform.position, _cam.transform.rotation, Subject);

        [Test]
        public void SignBetweenCameraAndSubject_IsHidden_ThenRestored()
        {
            var sign = Box("Tabela", new Vector3(-0.6f, 1.4f, -4f), new Vector3(1.6f, 2.4f, 0.3f));
            var o = new CinemaOccluders();
            Check(o);
            Assert.IsFalse(sign.enabled, "kralın önündeki tabela gizlenmeli");
            Assert.AreEqual(1, o.HiddenCount);
            o.Restore();
            Assert.IsTrue(sign.enabled, "sahne bitince geri gelmeli");
        }

        [Test]
        public void ObjectsBesideBehindOrLow_StayVisible()
        {
            var beside = Box("Yandaki", new Vector3(4.2f, 1.2f, -3.5f), new Vector3(0.6f, 2.4f, 0.3f));
            var behind = Box("Arkadaki", new Vector3(0f, 1.2f, 3f), new Vector3(1.6f, 2.4f, 0.3f));
            var low = Box("Sandalye", new Vector3(0f, 0.3f, -4f), new Vector3(0.8f, 0.6f, 0.8f));
            var o = new CinemaOccluders();
            Check(o);
            Assert.IsTrue(beside.enabled, "konunun yanındaki nesne kalmalı");
            Assert.IsTrue(behind.enabled, "konunun arkasındaki nesne kalmalı");
            Assert.IsTrue(low.enabled, "alçak nesne (sandalye, masa) gizlenmez");
            Assert.AreEqual(0, o.HiddenCount);
        }

        [Test]
        public void HugeObjectNearCamera_IsHidden_EvenOffSubject()
        {
            // kameranın dibinde, konunun yanında ama görüntünün büyük kısmını kaplayan ağaç
            var tree = Box("Agac", new Vector3(-2.2f, 3f, -6.2f), new Vector3(2.2f, 3f, 1.2f));
            var o = new CinemaOccluders();
            Check(o);
            Assert.IsFalse(tree.enabled);
        }

        [Test]
        public void Reset_ReevaluatesNextShot()
        {
            var sign = Box("Tabela", new Vector3(-0.6f, 1.4f, -4f), new Vector3(1.6f, 2.4f, 0.3f));
            var o = new CinemaOccluders();
            Check(o);
            Assert.IsFalse(sign.enabled);
            o.Reset();
            Assert.IsTrue(sign.enabled, "yeni çekimde önce geri açılır");
            // yeni çekim: kamera tabelanın arkasında (konuya bakar, tabela kameranın arkasında kalır)
            _cam.transform.position = new Vector3(0f, 3f, -2.5f);
            _cam.transform.LookAt(Subject);
            Check(o);
            Assert.IsTrue(sign.enabled, "görüntüde değilse görünür kalır");
        }
    }
}
