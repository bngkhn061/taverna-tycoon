using System;
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.1 son sahnenin dünya kısmı (oyuncu testi madde 25, GAME_DESIGN.md §15.2): Kral (Gizli Kral müşterisi, 30) önde bir şövalye
    /// ve arkada iki muhafızla sokaktan gelir, kapıda Barlo karşılar, masaya oturur ve kadeh kaldırır; sonra kamera tavernanın üstünde
    /// yavaşça döner. Kamera takibi (CameraFollow) bu sürede kapalıdır. Arayüz (yazılar, havai fişek, şerit, jenerik) HudV21'de.
    /// Kendi yürüyüş kodunu kullanır (müşteri döngüsüne girmez); bitince oluşturduğu her şeyi siler.
    /// </summary>
    public class EndingScene : MonoBehaviour
    {
        /// <summary>Sahnenin aşamaları (HUD her aşamada yazı / efekt değiştirir).</summary>
        public enum Phase { Arrive, Greet, ToTable, Toast, Gift, Orbit, Done }   // v2.4: Gift (kralın hediyesi)

        /// <summary>Kralın ve maiyetinin yürüyüş hızı (m/sn).</summary>
        const float WalkSpeed = 1.8f;
        /// <summary>Aşama süreleri (sn): karşılama, kadeh, kamera turu.</summary>
        const float GreetSeconds = 3.2f, ToastSeconds = 4f, OrbitSeconds = 9f;
        /// <summary>v2.4: hediye anı (sn): kral fermanı uzatır, kamera duvara döner, ferman tezgâhın üstünde belirir.</summary>
        public const float GiftSeconds = 6f;
        /// <summary>Kralın ve maiyetinin sokakta başladığı x (batı ucu, kapının solunda) ve sokağın z'si.</summary>
        const float StreetStartX = -9.5f, StreetZ = -6.5f;
        /// <summary>Kamera turu: tavernanın ortası, açı (derece), uzaklık ve dönüş genişliği.</summary>
        static readonly Vector3 OrbitCenter = new(1.5f, 0f, -1.2f);
        const float OrbitPitch = 56f, OrbitDistance = 19f, OrbitYaw = 55f;
        /// <summary>Kral modelinin büyütülmesi (maiyetin arasında seçilsin).</summary>
        const float KingScale = 1.15f;
        /// <summary>Gizli Kral, şövalye ve muhafız müşteri numaraları (CHARACTERS.md §B).</summary>
        const int KnightNumber = 16, GuardNumber = 10;
        /// <summary>Kralın kadehi: elin yanı (m), kalkış yüksekliği (omuz → başın hemen üstü) ve büyütme.</summary>
        const float CupSide = 0.42f, CupForward = 0.12f, CupLow = 0.8f, CupHigh = 1.2f, CupScale = 2.2f;

        class Actor
        {
            public Transform t;
            public ToyAnimator anim;
            public List<Vector3> path = new();
            public int idx;
            public float delay;
            public bool Arrived => idx >= path.Count;
        }

        /// <summary>Şu anki aşama.</summary>
        public Phase Current { get; private set; }
        /// <summary>Aşama değişince (HUD).</summary>
        public event Action<Phase> PhaseChanged;
        /// <summary>Aşamanın başından beri geçen süre (sn).</summary>
        public float PhaseTime { get; private set; }
        /// <summary>Sahnenin başından beri geçen süre (sn).</summary>
        public float TotalTime { get; private set; }

        Actor _king;
        readonly List<Actor> _retinue = new();
        readonly List<GameObject> _spawned = new();
        Camera _cam;
        CameraFollow _follow;
        Vector3 _seatPos, _tablePos, _greetPos, _barloPos;
        List<Vector3> _innerPath;
        Transform _cup;
        Vector3 _camFocus;
        float _camPitch = 40f, _camDist = 12f, _camYaw;
        /// <summary>v2.5: kameranın önüne giren büyük nesneler (tabela, sancak, sütun…) çekim boyunca gizlenir.</summary>
        CinemaOccluders _occluders;
        /// <summary>Test günlüğü: gizlenen nesne sayısı ve adları.</summary>
        public string OccluderInfo => _occluders != null ? $"{_occluders.HiddenCount} ({_occluders.HiddenNames})" : "-";

        /// <summary>Sahneyi kurar ve başlatır (kamera takibi kapanır).</summary>
        public static EndingScene Play(Camera cam)
        {
            var go = new GameObject("EndingScene");
            var s = go.AddComponent<EndingScene>();
            s.Setup(cam);
            return s;
        }

        void Setup(Camera cam)
        {
            _cam = cam;
            _follow = cam ? cam.GetComponent<CameraFollow>() : null;
            if (_follow) _follow.enabled = false;
            var spawner = FindAnyObjectByType<CustomerSpawner>();
            Vector3 ent = spawner && spawner.entrance ? spawner.entrance.position : new Vector3(0f, 0f, -5.45f);
            Vector3 hub = spawner && spawner.hub ? spawner.hub.position : new Vector3(0.6f, 0f, 1.2f);
            ent.y = hub.y = 0f;
            _greetPos = ent + new Vector3(0f, 0f, 1.0f);
            _barloPos = _greetPos + new Vector3(0.95f, 0f, 0.55f);

            // Kralın masası: meyhanedeki, kapıya en yakın, boş oturağı olan masa
            PickSeat(ent, hub);

            var street = new Vector3(ent.x, 0f, StreetZ);
            // v2.4: taçlı, cüppeli gerçek Kral modeli (CH_King; giriş sinemasındaki kralın aynısı). Yoksa eski "Gizli Kral" müşterisi.
            _king = MakeKing(new Vector3(StreetStartX, 0f, StreetZ)) ?? MakeActor(TavernEvents.KingCustomerNumber, new Vector3(StreetStartX, 0f, StreetZ), KingScale);
            if (RoyalDecreeDecor.I) { RoyalDecreeDecor.I.forceHidden = true; RoyalDecreeDecor.I.Refresh(); }   // hediye anına kadar duvarda yok
            if (_king != null) { _king.path.AddRange(new[] { street, ent, _greetPos }); }
            // Şövalye önde (kapıdan girip kralın soluna geçer), iki muhafız arkada (kapının iki yanında nöbet)
            AddFollower(KnightNumber, -1.3f, new[] { street, ent, _greetPos + new Vector3(-1.0f, 0f, 0.5f) });
            AddFollower(GuardNumber, 1.4f, new[] { street, ent + new Vector3(-0.75f, 0f, 0.15f) });
            AddFollower(GuardNumber, 2.2f, new[] { street, ent + new Vector3(0.75f, 0f, 0.15f) });

            // Barlo kapının içinde, kralı bekler
            var p = PlayerController.I;
            if (p)
            {
                p.Teleport(_barloPos);
                FaceTo(p.transform, street);
            }
            _camFocus = new Vector3(StreetStartX * 0.4f, 0f, StreetZ + 1f);
            _occluders = new CinemaOccluders();
            Current = Phase.Arrive;
        }

        void PickSeat(Vector3 ent, Vector3 hub)
        {
            Table best = null;
            Seat bestSeat = null;
            float bestD = float.MaxValue;
            foreach (var t in Table.All)
            {
                if (!t || Inn.Upstairs(t.transform.position)) continue;
                var tp = t.transform.position;
                if (Mathf.Abs(tp.x - ent.x) > 3.5f) continue;   // ana meyhane
                foreach (var s in t.seats)
                {
                    if (!s || !s.Free) continue;
                    float d = Vector3.Distance(s.transform.position, ent);
                    if (d < bestD) { bestD = d; best = t; bestSeat = s; }
                }
            }
            _innerPath = new List<Vector3>();
            if (best && bestSeat)
            {
                _tablePos = best.transform.position;
                _seatPos = bestSeat.transform.position;
                if (bestSeat.approach != null) foreach (var a in bestSeat.approach) if (a) _innerPath.Add(a.position);
                if (_innerPath.Count == 0) _innerPath.Add(new Vector3(_seatPos.x, 0f, Mathf.Min(_seatPos.z, _greetPos.z + 0.6f)));
            }
            else
            {
                _tablePos = hub + new Vector3(0f, 0f, -1.5f);
                _seatPos = _tablePos + new Vector3(0f, 0f, -0.7f);
                _innerPath.Add(new Vector3(_seatPos.x, 0f, _greetPos.z + 0.6f));
            }
            _innerPath.Add(_seatPos);
        }

        void AddFollower(int number, float delay, Vector3[] path)
        {
            var a = MakeActor(number, new Vector3(StreetStartX - delay * WalkSpeed, 0f, StreetZ), 1f);
            if (a == null) return;
            a.delay = Mathf.Max(0f, delay);
            a.path.AddRange(path);
            _retinue.Add(a);
        }

        /// <summary>v2.4: CinemaProps'taki Kral modeli.</summary>
        Actor MakeKing(Vector3 pos)
        {
            var pf = CinemaProps.I ? CinemaProps.I.king : null;
            if (!pf) return null;
            var go = new GameObject("Ending_Kral");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var m = Instantiate(pf, go.transform);
            m.transform.localPosition = Vector3.zero;
            m.transform.localScale *= 1.05f;
            if (!m.TryGetComponent(out ToyAnimator anim)) anim = m.AddComponent<ToyAnimator>();
            _spawned.Add(go);
            return new Actor { t = go.transform, anim = anim };
        }

        Actor MakeActor(int number, Vector3 pos, float scale)
        {
            var db = GameDatabase.I;
            CustomerDef def = null;
            if (db) foreach (var c in db.customers) if (c && c.number == number) { def = c; break; }
            if (!def || !def.modelPrefab) return null;
            var go = new GameObject($"Ending_{def.displayName}");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var m = Instantiate(def.modelPrefab, go.transform);
            m.transform.localPosition = Vector3.zero;
            m.transform.localScale *= scale;
            if (!m.TryGetComponent(out ToyAnimator anim)) anim = m.AddComponent<ToyAnimator>();
            _spawned.Add(go);
            return new Actor { t = go.transform, anim = anim };
        }

        static void FaceTo(Transform t, Vector3 target)
        {
            var to = target - t.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.001f) t.rotation = Quaternion.LookRotation(to);
        }

        /// <summary>Aktörü yolunda bir adım yürütür; vardıysa true.</summary>
        static bool Walk(Actor a, float dt)
        {
            if (a == null) return true;
            if (a.delay > 0f) { a.delay -= dt; a.anim.SetSpeed(0f); return false; }
            if (a.Arrived) { a.anim.SetSpeed(0f); return true; }
            var target = a.path[a.idx];
            target.y = a.t.position.y;
            var to = target - a.t.position;
            float step = WalkSpeed * dt;
            if (to.magnitude <= step) { a.t.position = target; a.idx++; }
            else
            {
                a.t.position += to.normalized * step;
                a.t.rotation = Quaternion.RotateTowards(a.t.rotation, Quaternion.LookRotation(to), 540f * dt);
            }
            a.anim.SetSpeed(1f);
            return false;
        }

        void Next(Phase p)
        {
            _occluders?.Reset();   // v2.5: her çekim kendi önünü açar (önceki çekimde kalkan tabela, sonrakinde engel değilse geri gelir)
            Current = p;
            PhaseTime = 0f;
            PhaseChanged?.Invoke(p);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            TotalTime += dt;
            PhaseTime += dt;
            var barlo = PlayerController.I;
            foreach (var r in _retinue) Walk(r, dt);
            switch (Current)
            {
                case Phase.Arrive:
                    if (barlo && _king != null) FaceTo(barlo.transform, _king.t.position);
                    if (Walk(_king, dt) || _king == null)
                    {
                        if (_king != null && barlo) FaceTo(_king.t, barlo.transform.position);
                        if (barlo && barlo.anim) barlo.anim.Hop();
                        Sfx.Play(SfxId.Claim);
                        Next(Phase.Greet);
                    }
                    break;
                case Phase.Greet:
                    if (_king != null && PhaseTime > 0.6f && PhaseTime - dt <= 0.6f) _king.anim.Pop();
                    if (barlo && PhaseTime > 1.4f && PhaseTime - dt <= 1.4f && barlo.anim) barlo.anim.Hop();
                    if (PhaseTime >= GreetSeconds)
                    {
                        if (_king != null) { _king.path.AddRange(_innerPath); }
                        Next(Phase.ToTable);
                    }
                    break;
                case Phase.ToTable:
                    if (barlo && _king != null) FaceTo(barlo.transform, _king.t.position);
                    if (Walk(_king, dt) || _king == null)
                    {
                        if (_king != null) FaceTo(_king.t, _tablePos);
                        RaiseCup();
                        Sfx.Play(SfxId.Clink);
                        Next(Phase.Toast);
                    }
                    break;
                case Phase.Toast:
                    if (_cup)
                    {
                        // kadeh yavaşça başın üstüne kalkar, iki kez "şerefe" sallanır
                        float k = Mathf.Clamp01(PhaseTime / 1.2f);
                        _cup.localPosition = new Vector3(CupSide, Mathf.Lerp(CupLow, CupHigh, k) + Mathf.Sin(PhaseTime * 9f) * 0.04f * k, CupForward);
                    }
                    if (PhaseTime > 1.3f && PhaseTime - dt <= 1.3f) { if (_king != null) _king.anim.Hop(); if (barlo && barlo.anim) barlo.anim.Hop(); Sfx.Play(SfxId.Clink, 1.1f); }
                    if (PhaseTime >= ToastSeconds) { Next(Phase.Gift); if (_king != null) _king.anim.Pop(); Sfx.Play(SfxId.Claim); }
                    break;
                case Phase.Gift:
                    // v2.4: kral fermanı uzatır (0.4 sn), kamera duvara döner (1.6 sn), ferman tezgâhın üstünde belirir (2.2 sn)
                    if (PhaseTime > 0.4f && PhaseTime - dt <= 0.4f) RaiseScroll();
                    if (_giftScroll) _giftScroll.localPosition = new Vector3(CupSide * -1f, 1.0f + Mathf.Sin(PhaseTime * 5f) * 0.03f, 0.35f);
                    if (PhaseTime > 2.2f && PhaseTime - dt <= 2.2f)
                    {
                        if (RoyalDecreeDecor.I) RoyalDecreeDecor.I.Reveal();
                        Sfx.Play(SfxId.Unlock);
                        if (barlo && barlo.anim) barlo.anim.Hop();
                    }
                    if (PhaseTime >= GiftSeconds) { Sfx.Play(SfxId.LevelUp); Next(Phase.Orbit); }
                    break;
                case Phase.Orbit:
                    if (PhaseTime > 2f && PhaseTime - dt <= 2f && _king != null) _king.anim.Hop();
                    if (PhaseTime >= OrbitSeconds) Next(Phase.Done);
                    break;
            }
            UpdateCamera(dt);
        }

        Transform _giftScroll;

        /// <summary>v2.4: kralın elinde ferman rulosu.</summary>
        void RaiseScroll()
        {
            var pf = CinemaProps.I ? CinemaProps.I.scroll : null;
            if (_king == null || !pf) return;
            _giftScroll = Instantiate(pf, _king.t).transform;
            _giftScroll.localPosition = new Vector3(-CupSide, 1.0f, 0.35f);
            _giftScroll.localRotation = Quaternion.identity;
            _giftScroll.localScale = Vector3.one * 1.6f;
        }

        void RaiseCup()
        {
            if (_king == null) return;
            var def = GameDatabase.I ? GameDatabase.I.Drink(DrinkId.KralSerbeti) : null;
            if (!def || !def.cupPrefab) def = GameDatabase.I ? GameDatabase.I.Drink(DrinkId.Bira) : null;
            if (!def || !def.cupPrefab) return;
            _cup = Instantiate(def.cupPrefab, _king.t).transform;
            _cup.localPosition = new Vector3(CupSide, CupLow, CupForward);
            _cup.localScale = Vector3.one * CupScale;
        }

        void UpdateCamera(float dt)
        {
            if (!_cam) return;
            Vector3 focus;
            float pitch, dist, yaw = 0f;
            Vector3 kp = _king != null ? _king.t.position : _greetPos;
            switch (Current)
            {
                case Phase.Arrive:
                    focus = new Vector3(Mathf.Clamp(kp.x, -6f, 2f), 0f, -5.2f);
                    pitch = 34f; dist = 11.5f; break;
                case Phase.Greet:
                    focus = (kp + _barloPos) * 0.5f + new Vector3(0f, 0.4f, 0f);
                    pitch = 40f; dist = 8f; break;
                case Phase.ToTable:
                case Phase.Toast:
                    focus = kp + new Vector3(0f, 0.5f, 0f);
                    pitch = 46f; dist = 8.5f; break;
                case Phase.Gift:
                    // önce kral, 1.6 sn'den sonra duvardaki ferman
                    var wall = RoyalDecreeDecor.I ? RoyalDecreeDecor.I.Center : kp + new Vector3(0f, 1.5f, 2f);
                    focus = PhaseTime < 1.6f ? kp + new Vector3(0f, 0.7f, 0f) : wall + new Vector3(0f, -0.3f, 0f);
                    pitch = PhaseTime < 1.6f ? 30f : 18f; dist = PhaseTime < 1.6f ? 6.5f : 6f; break;
                default:
                    float k = Mathf.Clamp01(PhaseTime / OrbitSeconds);
                    float e = k * k * (3f - 2f * k);
                    focus = OrbitCenter;
                    pitch = Mathf.Lerp(48f, OrbitPitch, e);
                    dist = Mathf.Lerp(11f, OrbitDistance, e);
                    yaw = Mathf.Lerp(-OrbitYaw * 0.4f, OrbitYaw, e);
                    break;
            }
            // v2.5: hedef duruşu önceden yokla (nesne kamera oraya varmadan kalksın); hediyede duvar ve kral çekimleri ikisi de
            // Gelişte kamera sokağın arkasına bakar ama konu kral: önü kapatan yalnızca kralla kamera arasındaki nesne (arkadaki tabela kalır)
            Vector3 subject = Current == Phase.Arrive ? kp + new Vector3(0f, 0.8f, 0f) : focus;
            CheckPose(focus, pitch, dist, yaw, subject);
            if (Current == Phase.Gift && RoyalDecreeDecor.I) { var w = RoyalDecreeDecor.I.Center + new Vector3(0f, -0.3f, 0f); CheckPose(w, 18f, 6f, 0f, w); }
            if (Current == Phase.ToTable) { var st = _seatPos + new Vector3(0f, 0.5f, 0f); CheckPose(st, 46f, 8.5f, 0f, st); }
            float s = 1f - Mathf.Exp(-3f * dt);
            _camFocus = Vector3.Lerp(_camFocus, focus, s);
            _camPitch = Mathf.Lerp(_camPitch, pitch, s);
            _camDist = Mathf.Lerp(_camDist, dist, s);
            _camYaw = Current == Phase.Orbit || Current == Phase.Done ? yaw : Mathf.Lerp(_camYaw, yaw, s);
            var rot = Quaternion.Euler(_camPitch, _camYaw, 0f);
            _cam.transform.SetPositionAndRotation(_camFocus - rot * Vector3.forward * _camDist, rot);
            _occluders?.Check(_cam, _cam.transform.position, rot, Current == Phase.Arrive ? subject : _camFocus);
        }

        void CheckPose(Vector3 focus, float pitch, float dist, float yaw, Vector3 subject)
        {
            if (_occluders == null) return;
            var r = Quaternion.Euler(pitch, yaw, 0f);
            _occluders.Check(_cam, focus - r * Vector3.forward * dist, r, subject);
        }

        /// <summary>Oluşturulan aktörleri siler, kamera takibini geri açar ve kamerayı Barlo'ya oturtur.</summary>
        public void Cleanup()
        {
            foreach (var g in _spawned) if (g) Destroy(g);
            _spawned.Clear();
            _occluders?.Restore();   // v2.5: gizlenen tabela / sancak / sütun geri
            _occluders = null;
            if (RoyalDecreeDecor.I) { RoyalDecreeDecor.I.forceHidden = false; RoyalDecreeDecor.I.Refresh(); }   // v2.4: atlanırsa da görünür
            if (_follow) { _follow.enabled = true; _follow.SnapNow(); }
            Destroy(gameObject);
        }

        void OnDestroy() => _occluders?.Restore();

        /// <summary>Test günlüğü: aktör sayısı ve kralın konumu.</summary>
        public string DebugInfo => $"aşama {Current} · {TotalTime:0.0} sn · maiyet {_retinue.Count} · kral {(_king != null ? _king.t.position.ToString("F1") : "yok")} · oturak {_seatPos:F1}";
    }
}
