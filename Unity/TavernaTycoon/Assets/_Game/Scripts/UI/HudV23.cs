using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v2.3 Paket 5 arayüzü (GAME_DESIGN.md §17): sol sütunda "DÜZENLE" düğmesi (Sv5, ilk açılışta ipucu ve "YENİ!"),
    /// düzenleme modunda HUD gizlenir; üstte başlık ve ipucu, altta DÖNDÜR / VARSAYILANA DÖN / İPTAL / BİTTİ, dünyaya dokunuşları
    /// alan tam ekran katman (<see cref="EditLayout"/>). Ayarlar › Bulut kaydı buluttan yükleyince sahne yeniden yüklenir.
    /// </summary>
    public partial class HudController
    {
        Button _editBtn;
        Label _editNew, _editMsg, _editSel;
        VisualElement _editRoot;
        int _editPtr = -1;
        float _editMsgT;

        /// <summary>İpucu en erken bu kadar sn sonra (giriş animasyonu bitsin).</summary>
        const float EditTipDelay = 5f;
        /// <summary>Düzenleme mesajının ekranda kalma süresi (gerçek sn).</summary>
        const float EditMsgSeconds = 2.6f;
        /// <summary>İlk ipucu bildiriminin ekranda kalma süresi (sn). v2.5: 5 → 3.5 (ipucu şeridi kısa sürsün).</summary>
        const float EditTipSeconds = 3.5f;
        /// <summary>"YENİ!" rozetinin görüldü anahtarı (seenItems).</summary>
        const string EditSeenKey = "edit_mode";

        /// <summary>Test botu: düğme görünüyor mu, düzenleme mesajı, bulut paneli.</summary>
        public bool EditButtonVisible => _editBtn != null && !_editBtn.ClassListContains("hidden");
        public string EditMessage => _editMsg != null ? _editMsg.text : "";
        public CloudSavePanel Cloud => _settings?.Cloud;

        void InitV23()
        {
            _editBtn = new Button(EditPressed) { name = "edit-btn" };
            _editBtn.AddToClassList("btn"); _editBtn.AddToClassList("btn-sm"); _editBtn.AddToClassList("helper-btn");
            _editBtn.AddToClassList("btn-paper"); _editBtn.AddToClassList("edit-open-btn"); _editBtn.AddToClassList("tool-btn"); _editBtn.AddToClassList("hidden");   // v2.5 araç kutusu
            var ic = new VisualElement { pickingMode = PickingMode.Ignore };
            ic.AddToClassList("price-icon");
            ic.style.backgroundImage = new StyleBackground(KitIcon("hammer"));
            _editBtn.Add(ic);
            _editBtn.Add(new Label(Loc.T("DÜZENLE")) { pickingMode = PickingMode.Ignore });
            _editNew = new Label(Loc.T("YENİ!")) { pickingMode = PickingMode.Ignore };
            _editNew.AddToClassList("event-new");
            _editNew.AddToClassList("hidden");
            _editBtn.Add(_editNew);
            _eventBtn.parent.Add(_editBtn);
            BuildEditOverlay();
            EditLayout.ActiveChanged += OnEditActive;
            EditLayout.Message += OnEditMessage;
            _settings.CloudRestored += () => { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); };
        }

        void DisableV23()
        {
            EditLayout.ActiveChanged -= OnEditActive;
            EditLayout.Message -= OnEditMessage;
        }

        void BuildEditOverlay()
        {
            _editRoot = new VisualElement { name = "edit-root" };
            _editRoot.AddToClassList("screen");
            _editRoot.AddToClassList("hidden");
            var catcher = new VisualElement { name = "edit-catcher" };
            catcher.AddToClassList("screen");
            catcher.RegisterCallback<PointerDownEvent>(e =>
            {
                if (_editPtr != -1 || !EditLayout.Active) return;
                _editPtr = e.pointerId;
                catcher.CapturePointer(e.pointerId);
                EditLayout.I.PointerDown(PanelToScreen(e.position));
            });
            catcher.RegisterCallback<PointerMoveEvent>(e => { if (e.pointerId == _editPtr && EditLayout.Active) EditLayout.I.PointerMove(PanelToScreen(e.position)); });
            void Up(int id)
            {
                if (id != _editPtr) return;
                catcher.ReleasePointer(id);
                _editPtr = -1;
                if (EditLayout.I) EditLayout.I.PointerUp();
            }
            catcher.RegisterCallback<PointerUpEvent>(e => Up(e.pointerId));
            catcher.RegisterCallback<PointerCancelEvent>(e => Up(e.pointerId));
            _editRoot.Add(catcher);

            var top = new VisualElement { pickingMode = PickingMode.Ignore };
            top.AddToClassList("edit-top");
            top.AddToClassList("v3-panel");
            var title = new Label(Loc.T("DÜZENLEME MODU")) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("edit-title");
            top.Add(title);
            var hint = new Label(Loc.T("Sarı izli eşyayı sürükle: taşı · Boş yeri sürükle: kamerayı kaydır")) { pickingMode = PickingMode.Ignore };
            hint.AddToClassList("edit-hint");
            top.Add(hint);
            _editSel = new Label { pickingMode = PickingMode.Ignore };
            _editSel.AddToClassList("edit-hint");
            _editSel.AddToClassList("edit-sel");
            top.Add(_editSel);
            _editRoot.Add(top);

            _editMsg = new Label { pickingMode = PickingMode.Ignore };
            _editMsg.AddToClassList("edit-msg");
            _editMsg.AddToClassList("hidden");
            _editRoot.Add(_editMsg);

            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("edit-bar");
            bar.Add(EditBarButton(Loc.T("DÖNDÜR"), "btn-plum", "edit-rotate", () => EditLayout.I?.Rotate()));
            bar.Add(EditBarButton(Loc.T("VARSAYILANA DÖN"), "btn-paper", "edit-reset", () => EditLayout.I?.ResetDefaults()));
            bar.Add(EditBarButton(Loc.T("İPTAL"), "btn-paper", "edit-cancel", () => EditLayout.I?.Close(false)));
            bar.Add(EditBarButton(Loc.T("BİTTİ"), "btn-teal", "edit-done", () => EditLayout.I?.Close(true)));
            _editRoot.Add(bar);
            _root.Add(_editRoot);
        }

        static Button EditBarButton(string text, string variant, string name, System.Action click)
        {
            var b = new Button(() => { Sfx.Play(SfxId.Pop); click(); }) { name = name };
            b.AddToClassList("btn");
            b.AddToClassList(variant);
            b.AddToClassList("edit-bar-btn");
            b.Add(new Label(text));
            return b;
        }

        /// <summary>Panel koordinatından ekran pikseline (EditLayout kamerayla ışın atar).</summary>
        Vector2 PanelToScreen(Vector2 p)
        {
            var r = _root.layout;
            float sw = RenderTargetMode && cam ? cam.pixelWidth : Screen.width, sh = RenderTargetMode && cam ? cam.pixelHeight : Screen.height;   // v2.5 penceresiz test
            float sx = r.width > 1f ? sw / r.width : 1f, sy = r.height > 1f ? sh / r.height : 1f;
            return new Vector2(p.x * sx, sh - p.y * sy);
        }

        void EditPressed()
        {
            if (EditLayout.Active || !EditLayout.I) return;
            // v2.5: "YENİ!" ilk dokunuşta kalkar (açılamasa da: oyuncu düğmeyi gördü)
            if (!GameState.Data.seenItems.Contains(EditSeenKey)) { GameState.Data.seenItems.Add(EditSeenKey); GameState.Save(); }
            if (!EditLayout.CanOpen)
            {
                Toast(GameState.Level < Layout.UnlockLevel ? Loc.F("Seviye {0} gerekli", Layout.UnlockLevel) : Loc.T("Şu an düzenlenemez: gün sürerken dene"));
                return;
            }
            DebugOpen("close");
            var seen = GameState.Data.seenItems;
            if (!seen.Contains(EditSeenKey)) seen.Add(EditSeenKey);
            EditLayout.I.Open();
        }

        void OnEditActive(bool on)
        {
            _hudRoot.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
            _world.style.display = on ? DisplayStyle.None : DisplayStyle.Flex;
            _editRoot.EnableInClassList("hidden", !on);
            _editMsg.AddToClassList("hidden");
            _editPtr = -1;
            if (!on) { RefreshTop(); Toast(EditLayout.LastCloseSaved ? Loc.T("Yerleşim kaydedildi") : Loc.T("Değişiklikler geri alındı")); }
        }

        void OnEditMessage(string text, bool error)
        {
            _editMsg.text = text;
            _editMsg.EnableInClassList("err", error);
            _editMsg.RemoveFromClassList("hidden");
            _editMsgT = EditMsgSeconds;
            if (error) Sfx.Play(SfxId.Pop, 0.6f);
        }

        void UpdateV23()
        {
            if (_editBtn == null) return;
            bool show = GameState.Level >= Layout.UnlockLevel && !Tutorial.Active && !Ending.Playing && !DayCycle.Ended && !EditLayout.Active;
            _editBtn.EnableInClassList("hidden", !show);
            _editNew.EnableInClassList("hidden", GameState.Data.seenItems.Contains(EditSeenKey));
            var d = GameState.Data;
            if (show && !d.editTipShown && CalmForTip && Time.timeSinceLevelLoad > EditTipDelay)   // v2.5: başka şerit / bildirim yokken
            {
                d.editTipShown = true;
                Toast(Loc.T("Yeni: DÜZENLE ile masaları ve dekoru istediğin yere taşıyabilirsin"));
                _toastT = EditTipSeconds;
                GameState.Save();
            }
            if (!EditLayout.Active) return;
            if (_editMsgT > 0f) { _editMsgT -= Time.unscaledDeltaTime; if (_editMsgT <= 0f) _editMsg.AddToClassList("hidden"); }
            var el = EditLayout.I;
            _editSel.text = !el.HasSelection ? Loc.T("Bir masaya ya da dekora dokun") : el.SelectionValid ? Loc.T("Buraya konabilir") : Loc.T("Buraya konamaz");
            _editSel.EnableInClassList("bad", el.HasSelection && !el.SelectionValid);
        }

        /// <summary>Test botu: "DÜZENLE" düğmesine basar.</summary>
        public void DebugEditPress() => EditPressed();
    }
}
