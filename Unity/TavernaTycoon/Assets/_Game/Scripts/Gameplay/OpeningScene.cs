using System;
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.4 giriş sinemasının dünya kısmı (GAME_DESIGN.md §18.2). Oyuncu karakteri gizlenir, yerine sahneye özel bir Barlo modeli oynar
    /// (gerçek oyuncu ve kayıt hiç değişmez; yeniden izlemede de güvenli). Beş çekim:
    /// <list type="number">
    /// <item>Journey — uzaktaki giriş sahnesinde (<see cref="CinemaProps.stage"/>) Barlo el arabasını çekerek yolda yürür.</item>
    /// <item>Ruin — harap hanın önünde durur, şaşkın zıplar.</item>
    /// <item>Palace — saray köşesi: kırmızı halıda kralın önüne yürür, eğilir.</item>
    /// <item>Decree — Kral fermanı uzatır, rulo Barlo'ya süzülür (arayüzde ferman kartı, oyuncunun seçtiği adla).</item>
    /// <item>Sign — gerçek tavernanın önü: merdiven, tabela yaylı yerine oturur, Barlo sevinir.</item>
    /// </list>
    /// Çekimler arası karartma <see cref="Fade"/> (HUD siyah perdeyi buna göre açar). Kamera takibi bu sürede kapalı. Bitince her şey silinir.
    /// </summary>
    public class OpeningScene : MonoBehaviour
    {
        public enum Phase { Journey, Ruin, Palace, Decree, Sign, Done }

        /// <summary>Çekim süreleri (sn). Toplam ≈ 29.5 sn.</summary>
        public static readonly float[] Durations = { 6.5f, 4.5f, 6f, 6.5f, 6f };
        /// <summary>Karartma geçişi (sn).</summary>
        const float FadeSeconds = 0.55f;
        const float WalkSpeed = 2.2f, PalaceWalkSpeed = 1.7f;

        public Phase Current { get; private set; }
        public float PhaseTime { get; private set; }
        public float TotalTime { get; private set; }
        public event Action<Phase> PhaseChanged;
        /// <summary>Kararma (0 açık, 1 siyah).</summary>
        public float Fade { get; private set; } = 1f;
        /// <summary>Tabela yerine oturdu mu (HUD ad etiketini gösterir).</summary>
        public bool SignShown { get; private set; }
        /// <summary>Toplam süre.</summary>
        public static float TotalSeconds { get { float t = 0f; foreach (var d in Durations) t += d; return t; } }

        CinemaProps _p;
        Camera _cam;
        CameraFollow _follow;
        bool _stageWasActive;
        readonly List<GameObject> _spawned = new();
        readonly List<Renderer> _hiddenPlayer = new();
        Transform _barlo, _cart, _king, _scroll, _sign, _ladder;
        ToyAnimator _barloAnim, _kingAnim;
        Vector3 _signScale = Vector3.one, _camFocus, _scrollFrom;
        float _camPitch = 24f, _camDist = 9f, _camYaw;
        bool _snap = true;

        public static OpeningScene Play(Camera cam)
        {
            if (!CinemaProps.I || !CinemaProps.I.stage) return null;
            var s = new GameObject("OpeningScene").AddComponent<OpeningScene>();
            s.Setup(cam);
            return s;
        }

        void Setup(Camera cam)
        {
            _p = CinemaProps.I;
            _cam = cam;
            _follow = cam ? cam.GetComponent<CameraFollow>() : null;
            if (_follow) _follow.enabled = false;
            _stageWasActive = _p.stage.activeSelf;
            _p.stage.SetActive(true);
            // gerçek oyuncu görünmez (yerine sahne Barlo'su)
            var pl = PlayerController.I;
            if (pl) foreach (var r in pl.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; _hiddenPlayer.Add(r); }
            _barlo = Spawn(_p.barlo, _p.roadStart.position, Quaternion.LookRotation(Vector3.right), out _barloAnim);
            _cart = Spawn(_p.cart, _p.roadStart.position - Vector3.right * 1.1f, Quaternion.LookRotation(-Vector3.right), out _);
            // tabela: SignTiers grubunun etkin kademesi (grup dünyanın kökünde; ölçek kademeye uygulanır)
            var sg = GameObject.Find("SignTiers");
            if (sg) foreach (Transform c in sg.transform) if (c.gameObject.activeSelf) { _sign = c; break; }
            if (_sign) _signScale = _sign.localScale;
            Enter(Phase.Journey);
        }

        Transform Spawn(GameObject prefab, Vector3 pos, Quaternion rot, out ToyAnimator anim)
        {
            anim = null;
            if (!prefab) return null;
            var root = new GameObject("Op_" + prefab.name);
            root.transform.SetPositionAndRotation(pos, rot);
            var m = Instantiate(prefab, root.transform);
            m.transform.localPosition = Vector3.zero;
            if (m.TryGetComponent<Collider>(out var c)) c.enabled = false;
            if (prefab.name.StartsWith("CH_") && !m.TryGetComponent(out anim)) anim = m.AddComponent<ToyAnimator>();
            _spawned.Add(root);
            return root.transform;
        }

        void Enter(Phase p)
        {
            Current = p;
            PhaseTime = 0f;
            _snap = true;
            switch (p)
            {
                case Phase.Palace:
                    Place(_barlo, _p.carpetStart.position, Vector3.forward);
                    if (_cart) _cart.gameObject.SetActive(false);
                    _king = Spawn(_p.king, _p.kingSpot.position, _p.kingSpot.rotation, out _kingAnim);
                    Spawn(_p.guard, _p.guardL.position, _p.guardL.rotation, out _);
                    Spawn(_p.guard, _p.guardR.position, _p.guardR.rotation, out _);
                    Sfx.Play(SfxId.Door);
                    break;
                case Phase.Sign:
                    _p.stage.SetActive(false);
                    if (_sign)
                    {
                        var s = _sign.position;
                        Place(_barlo, new Vector3(s.x + 0.85f, 0f, s.z - 1.0f), -Vector3.forward);
                        _ladder = Spawn(_p.ladder, new Vector3(s.x + 0.2f, 0f, s.z - 0.3f), Quaternion.identity, out _);
                        _sign.localScale = Vector3.zero;
                    }
                    break;
            }
            PhaseChanged?.Invoke(p);
        }

        static void Place(Transform t, Vector3 pos, Vector3 face)
        {
            if (!t) return;
            t.position = pos;
            t.rotation = Quaternion.LookRotation(face);
        }

        /// <summary>Bir adım yürür; vardıysa true.</summary>
        bool Walk(Transform t, ToyAnimator a, Vector3 target, float speed, float dt)
        {
            if (!t) return true;
            target.y = t.position.y;
            var to = target - t.position;
            float step = speed * dt;
            if (to.magnitude <= step) { t.position = target; if (a) a.SetSpeed(0f); return true; }
            t.position += to.normalized * step;
            t.rotation = Quaternion.RotateTowards(t.rotation, Quaternion.LookRotation(to), 540f * dt);
            if (a) a.SetSpeed(1f);
            return false;
        }

        static bool At(float t, float dt, float mark) => t >= mark && t - dt < mark;

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            TotalTime += dt;
            PhaseTime += dt;
            float dur = Current < Phase.Done ? Durations[(int)Current] : 0f;
            // karartma: her çekimin başında açılır, sonunda kapanır (son çekimin sonu oyuna açılır)
            float fin = Mathf.Clamp01(1f - PhaseTime / FadeSeconds);
            float fout = Current == Phase.Sign ? 0f : Mathf.Clamp01((PhaseTime - (dur - FadeSeconds)) / FadeSeconds);
            Fade = Mathf.Max(fin, fout);
            float t = PhaseTime;
            switch (Current)
            {
                case Phase.Journey:
                    Walk(_barlo, _barloAnim, _p.ruinStop.position, WalkSpeed, dt);
                    FollowCart();
                    break;
                case Phase.Ruin:
                    if (Walk(_barlo, _barloAnim, _p.ruinStop.position, WalkSpeed, dt) && _barlo)
                        _barlo.rotation = Quaternion.RotateTowards(_barlo.rotation, Quaternion.LookRotation(Flat(_p.ruinFocus.position - _barlo.position)), 360f * dt);
                    FollowCart();
                    if (At(t, dt, 1.0f) && _barloAnim) { _barloAnim.Hop(); _barloAnim.Pop(); Sfx.Play(SfxId.Pop, 0.8f); }
                    if (At(t, dt, 2.3f) && _barloAnim) _barloAnim.Hop();
                    break;
                case Phase.Palace:
                    if (Walk(_barlo, _barloAnim, _p.bowSpot.position, PalaceWalkSpeed, dt) && _barlo && _king)
                    {
                        // krala dön ve eğil (ayak bileğinden öne 26°, 0.9 sn)
                        float bow = Mathf.Sin(Mathf.Clamp01((t - 3.9f) / 0.9f) * Mathf.PI) * 26f;
                        _barlo.rotation = Quaternion.LookRotation(Flat(_king.position - _barlo.position)) * Quaternion.Euler(bow, 0f, 0f);
                    }
                    if (At(t, dt, 3.9f) && _barloAnim) { _barloAnim.Pop(); Sfx.Play(SfxId.Step); }
                    if (At(t, dt, 4.6f) && _kingAnim) _kingAnim.Hop();
                    break;
                case Phase.Decree:
                    if (At(t, dt, 0.3f) && _kingAnim) { _kingAnim.Pop(); Sfx.Play(SfxId.Claim); }
                    if (At(t, dt, 0.6f) && _p.scroll && _king)
                    {
                        _scrollFrom = _king.position + _king.forward * 0.35f + Vector3.up * 0.85f;
                        _scroll = Spawn(_p.scroll, _scrollFrom, Quaternion.LookRotation(_king.forward), out _);
                        _scroll.localScale = Vector3.one * 1.4f;
                    }
                    if (_scroll && _barlo)
                    {
                        float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 1.4f) / 1.2f));
                        var to = _barlo.position + _barlo.forward * 0.3f + Vector3.up * 0.75f;
                        _scroll.position = Vector3.Lerp(_scrollFrom, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.35f;
                    }
                    if (At(t, dt, 2.7f) && _barloAnim) { _barloAnim.Hop(); Sfx.Play(SfxId.LevelUp); }
                    if (At(t, dt, 3.6f) && _barloAnim) _barloAnim.Hop();
                    break;
                case Phase.Sign:
                    if (_sign)
                    {
                        float k = Mathf.Clamp01((t - 1.0f) / 0.7f);
                        float s = k <= 0f ? 0f : k < 1f ? Back(k) : 1f;
                        _sign.localScale = _signScale * s;
                        if (At(t, dt, 1.0f)) Sfx.Play(SfxId.Unlock);
                        SignShown = k >= 1f;
                    }
                    if (At(t, dt, 1.8f) && _barloAnim) _barloAnim.Hop();
                    if (At(t, dt, 2.5f) && _barloAnim) { _barloAnim.Hop(); _barloAnim.Pop(); }
                    if (At(t, dt, 3.3f) && _barloAnim) _barloAnim.Hop();
                    break;
            }
            UpdateCamera(dt);
            if (Current < Phase.Done && PhaseTime >= dur)
            {
                if (Current == Phase.Sign) { Current = Phase.Done; PhaseChanged?.Invoke(Phase.Done); }
                else Enter(Current + 1);
            }
        }

        static float Back(float t) { const float s = 1.9f; t -= 1f; return t * t * ((s + 1f) * t + s) + 1f; }
        static Vector3 Flat(Vector3 v) { v.y = 0f; return v.sqrMagnitude > 0.0001f ? v : Vector3.forward; }

        void FollowCart()
        {
            if (!_cart || !_barlo) return;
            var back = -_barlo.forward;
            var target = _barlo.position + back * 1.15f;
            _cart.position = Vector3.Lerp(_cart.position, target, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            var dir = _barlo.position - _cart.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) _cart.rotation = Quaternion.LookRotation(-dir);   // kollar (−Z) Barlo'ya
        }

        void UpdateCamera(float dt)
        {
            if (!_cam) return;
            Vector3 focus;
            float pitch, dist, yaw;
            var b = _barlo ? _barlo.position : _p.roadStart.position;
            switch (Current)
            {
                case Phase.Journey:
                    focus = b + new Vector3(1.6f, 0.7f, 0.6f); pitch = 22f; dist = 9.5f; yaw = -8f; break;
                case Phase.Ruin:
                    focus = Vector3.Lerp(b, _p.ruinFocus.position, 0.6f) + new Vector3(0f, 0.6f, 0f); pitch = 22f; dist = 9.5f; yaw = -14f; break;
                case Phase.Palace:
                    focus = _p.palaceFocus.position + new Vector3(0f, 0.6f, Mathf.Lerp(-1.2f, 0f, Mathf.Clamp01(PhaseTime / 3f))); pitch = 24f; dist = 9f; yaw = 28f; break;
                case Phase.Decree:
                    // kral ve Barlo ekranın sağında kalsın (solda ferman kartı)
                    focus = (_king ? (_king.position + b) * 0.5f : b) + new Vector3(0f, 0.9f, 0f) + Quaternion.Euler(0f, 30f, 0f) * Vector3.left * 1.7f;
                    pitch = 16f; dist = 6.2f; yaw = 30f; break;
                default:
                    focus = (_sign ? _sign.position : b) + new Vector3(0.4f, 1.3f, 0f); pitch = 16f; dist = 7.2f; yaw = -10f; break;
            }
            float s = _snap ? 1f : 1f - Mathf.Exp(-2.5f * dt);
            _snap = false;
            _camFocus = Vector3.Lerp(_camFocus, focus, s);
            _camPitch = Mathf.Lerp(_camPitch, pitch, s);
            _camDist = Mathf.Lerp(_camDist, dist, s);
            _camYaw = Mathf.Lerp(_camYaw, yaw, s);
            var rot = Quaternion.Euler(_camPitch, _camYaw, 0f);
            _cam.transform.SetPositionAndRotation(_camFocus - rot * Vector3.forward * _camDist, rot);
        }

        /// <summary>Her şeyi siler, oyuncuyu ve tabelayı geri getirir, kamera takibini açar.</summary>
        public void Cleanup()
        {
            foreach (var g in _spawned) if (g) Destroy(g);
            _spawned.Clear();
            foreach (var r in _hiddenPlayer) if (r) r.enabled = true;
            _hiddenPlayer.Clear();
            if (_sign) _sign.localScale = _signScale;
            if (_p && _p.stage) _p.stage.SetActive(_stageWasActive);
            if (_follow) { _follow.enabled = true; _follow.SnapNow(); }
            Destroy(gameObject);
        }

        public string DebugInfo => $"çekim {Current} · {TotalTime:0.0} sn · karartma {Fade:0.00} · Barlo {(_barlo ? _barlo.position.ToString("F1") : "yok")} · kral {(_king ? "var" : "yok")} · tabela {(_sign ? _sign.localScale.x.ToString("0.00") : "yok")}";
    }
}
