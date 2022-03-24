using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Agent 
{ 
        public Transform transform;
        public bool isstable = true;
        public bool hasleader = false;
        public Vector3 target = Vector3.zero;
        public List<Vector3> path = new List<Vector3>();
        public Agent(Transform t) {
                transform = t;
        }
        public Vector3 Velocity = Vector3.zero;
}

