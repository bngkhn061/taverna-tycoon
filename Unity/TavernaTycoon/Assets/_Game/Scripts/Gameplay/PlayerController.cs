using UnityEngine;
using UnityEngine.InputSystem;

namespace Taverna
{
    /// <summary>Barlo: joystick (veya WASD/oklar) ile hareket. Etkileşimler yaklaşınca otomatik.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController I { get; private set; }

        /// <summary>HUD joystick'i buraya yazar (-1..1).</summary>
        public static Vector2 JoystickInput;

        public float speed = Balance.PlayerSpeed;
        public float turnSpeed = 900f;
        public ToyAnimator anim;
        public Carrier carrier;

        CharacterController _cc;
        Vector3 _vel;

        void Awake()
        {
            I = this;
            _cc = GetComponent<CharacterController>();
            if (!carrier) carrier = GetComponent<Carrier>();
        }

        /// <summary>Kat geçişi (merdiven): karakter denetleyicisi kapatılıp konum atanır.</summary>
        public void Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = true;
        }

        void Update()
        {
            Vector2 input = JoystickInput;
            var kb = Keyboard.current;
            if (kb != null && input.sqrMagnitude < 0.01f)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1;
            }
            input = Vector2.ClampMagnitude(input, 1f);
            if (Ending.Playing) input = Vector2.zero;   // v2.1: son sahnede Barlo'yu sahne yönetir
            // v2.1 Barlo'nun yetenekleri: koşu hızı ve taşıma kapasitesi kayıttan (BarloSkills)
            speed = BarloSkills.Speed;
            if (carrier) carrier.capacity = BarloSkills.Carry;

            // Kamera sabit ve +Z'ye bakıyor → girdi doğrudan dünya XZ düzlemine eşlenir.
            var move = new Vector3(input.x, 0f, input.y);
            _vel = Vector3.Lerp(_vel, move * speed, 1f - Mathf.Exp(-14f * Time.deltaTime));
            _cc.SimpleMove(_vel);

            if (move.sqrMagnitude > 0.01f)
            {
                var target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
            }
            if (anim) anim.SetSpeed(_vel.magnitude / speed);
        }
    }
}
