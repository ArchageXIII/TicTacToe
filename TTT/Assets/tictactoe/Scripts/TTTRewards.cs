using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TTT
{

    [CreateAssetMenu(fileName = "TTTRewards", menuName = "TTTRewards")]
    public class TTTRewards : ScriptableObject
    {
        public float PlayerXWin = 100.0f;
        public float PlayerXLost = -100.0f;
        public float PlayerXDraw = 50.0f;
        public float PlayerOWin = 100.0f;
        public float PlayerOLost = -100.0f;
        public float PlayerODraw = 50.0f;
        public float CouldHaveWon = -0.5f;
        public float CouldHaveBlocked = -0.5f;

    }


}
