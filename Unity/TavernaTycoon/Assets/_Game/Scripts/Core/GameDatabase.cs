using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Tüm tanımların tek kaynağı. Resources/GameDatabase.asset olarak durur;
    /// editördeki "Taverna/Kurulum" menüsü bunu otomatik doldurur.
    /// </summary>
    public class GameDatabase : ScriptableObject
    {
        public DrinkDef[] drinks;
        public CustomerDef[] customers;
        public GameObject coinPrefab;
        public Sprite coinIcon;
        public Sprite gemIcon;

        static GameDatabase _instance;
        public static GameDatabase I
        {
            get
            {
                if (_instance == null) _instance = Resources.Load<GameDatabase>("GameDatabase");
                return _instance;
            }
        }

        Dictionary<DrinkId, DrinkDef> _byId;
        public DrinkDef Drink(DrinkId id)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<DrinkId, DrinkDef>();
                foreach (var d in drinks) if (d != null) _byId[d.id] = d;
            }
            return _byId.TryGetValue(id, out var def) ? def : null;
        }
    }
}
