using DelightStudio.Data;
using System.Collections.Generic;
using UnityEngine;

namespace DelightStudio.Manager {
    public class TutorialManager : MonoBehaviour {
        [Header("Tutorials")]
        [SerializeField] private List<Tutorial_SO> tutorials = new();

        public List<Tutorial_SO> GetAllTutorials() => tutorials;
    }
}