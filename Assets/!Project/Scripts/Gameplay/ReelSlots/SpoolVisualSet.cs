using System;
using UnityEngine;
using UnityEngine.UI;

namespace AzurGames.Wool.Gameplay
{
    [Serializable]
    public class SpoolVisualSet
    {
        public BoxType Size;

        public GameObject Root;
        public Image SpoolImage;
        public Image[] CoilsImage;

        public Vector3 GetCoilPosition(int index)
        {
            if (index >= CoilsImage.Length)
                index = CoilsImage.Length - 1;

            return CoilsImage[index].transform.position;
        }
    }
}