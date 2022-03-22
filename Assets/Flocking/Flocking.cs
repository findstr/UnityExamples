using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flocking : MonoBehaviour
{
        public PathFinder PathFinder;
        public GameObject agent;
        public int agentCount = 0;
        List<Agent> pools = new List<Agent>();
        // Start is called before the first frame update
        void Start()
        {
                var n = PathFinder.WhichGridNode(agent.transform.position);
                Debug.Log("Flocking start:" + n.coord + ":" + agent.transform.position);
                for (int i = 0; i < agentCount; i++) {
                        var go = Instantiate(agent, transform) as GameObject;
                        go.SetActive(true);
                        var a = go.GetComponent<Agent>();
                        pools.Add(a);
                        a.Born(PathFinder, n);
                }
                Debug.Log("Flocking start2");
        }

        // Update is called once per frame
        void FixedUpdate()
        {
                for (int i = 0; i < pools.Count; i++) {
                        pools[i].Moving();
                }
        }
}
