using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Agent : MonoBehaviour
{ 
        public bool isstable = true;
        public float speed = 0.5f;
        public float colliderRadius = 1.0f;
        public float closeRadius = 1.5f;
        public float cohesionRadius = 3.5f;
        public Vector3 target = Vector3.zero;
        public List<Vector3> path = new List<Vector3>();
        public Vector3 Velocity = Vector3.zero;
        public bool Isrunning {
                get {
                        return animator.GetBool("isWalking");
                }
                set {
                        animator.SetBool("isWalking", value);
                }
        }
        private Animator animator;
	void Awake()
	{
                animator = GetComponent<Animator>();
	}
}

