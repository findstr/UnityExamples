using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flocking : MonoBehaviour
{
        public PathFinder PathFinder;
        public GameObject agent;
        public int agentCount = 0;
        private int close_idx = 1;
        List<Agent> pools = new List<Agent>();
        // Start is called before the first frame update
        void Start()
        {
                var n = PathFinder.WhichGridNode(agent.transform.position);
                Debug.Log("Flocking start:" + n.coord + ":" + agent.transform.position);
                for (int i = 0; i < agentCount; i++) {
                        var rnd = Random.insideUnitCircle;
                        var pos = transform.position + new Vector3(rnd.x, rnd.y, 0) * 2;
                        var go = Instantiate(agent, pos, Quaternion.identity, transform) as GameObject;
                        go.SetActive(true);
                        var a = go.GetComponent<Agent>();
                        pools.Add(a);
                        a.Born(PathFinder);
                }
                Debug.Log("Flocking start2");
        }

        // Update is called once per frame
        void FixedUpdate()
        {
                if (close_idx == PathFinder.close_idx)
                        return ;
                int moving = 0;
                for (int i = 0; i < pools.Count; i++) {
                        moving += pools[i].Moving(pools);
                }
                if (moving == 0)
                        close_idx = PathFinder.close_idx;
        }
}
