using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flocking : MonoBehaviour
{
        public PathFinder PathFinder;
        public GameObject templ;
        public int agentCount = 0;
        private Agent leader = null;
        private Vector3 target = Vector3.zero;
        private List<Agent> pools = new List<Agent>();
        private List<Vector3> teamSlot = new List<Vector3>();
        // Start is called before the first frame update
        void Start()
        {
                for (int i = 0; i < agentCount; i++) {
                        var rnd = Random.insideUnitCircle;
                        var pos = transform.position + new Vector3(rnd.x, rnd.y, 0) * 2;
                        var go = Instantiate(templ, pos, Quaternion.identity, transform) as GameObject;
                        go.SetActive(true);
                        go.name = i.ToString();
                        var a = new Agent(go.transform);
                        pools.Add(a);
                        PathFinder.Enter(pos, go);
                }
        }

        // Update is called once per frame
	void Update()
	{
		if (Input.GetMouseButtonDown(0)) {
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit, 30)) {
                                target = hit.point;
                                foreach (var agent in pools) {
                                        leader = null;
                                        agent.isstable = false;
                                        agent.target = target;
                                        PathFinder.Find(agent.transform.position, target, agent.path);
                                }
			}
		}
	}
        bool Colidering(Agent character, out Vector3 v) {
                int n = 0;
                float radius = 0.2f;
                float speed = 0.5f;
                v = Vector3.zero;
                foreach (var t in pools) {
                        if (t == character)
                                continue;
                        var dir = character.transform.position - t.transform.position;
                        var dist = dir.magnitude;
                        if (dist <= radius && dist > Mathf.Epsilon) {
                                ++n;
                                var strength = Mathf.Min(speed * (radius / dist), speed);
                                v += dir.normalized * strength;
                        } 
                }
                if (n == 0)
                        return false;
                v /= n;
                return true;
        }
        void Plan(Agent agent) {
                PathFinder.Find(agent.transform.position, agent.target, agent.path);
        }

        Vector3 Seperation(Agent character) {
                int n = 0;
                float min_radius = 0.2f;
                float max_radius = 0.3f;
                float speed = 0.5f;
                Vector3 v = Vector3.zero;
                foreach (var t in pools) {
                        if (t == character)
                                continue;
                        var dir =  character.transform.position - t.transform.position;
                        var dist = dir.magnitude;
                        if (dist > min_radius && dist <= max_radius) {
                                ++n;
                                var strength = Mathf.Min(speed * (max_radius / dist), speed * 2.0f);
                                v += dir.normalized * strength;
                        }
                }
                if (n > 0)
                        v /= n;
                return v;
        }

        Vector3 Cohersion(Agent character) {
                int n = 0;
                Vector3 center= Vector3.zero;
                const float radius = 0.8f;
                const float speed = 0.5f;
                foreach (var t in pools) {
                        if (t == character)
                                continue;
                        var dir = character.transform.position - t.transform.position;
                        var dist = dir.magnitude;
                        if (dist <= radius) {
                                ++n;
                                center += t.transform.position;
                        }
                }
                if (n == 0)
                        return Vector3.zero;
                center /= n;
                var d = center - character.transform.position;
                if (d.magnitude <= 0.4f)
                        return Vector3.zero;
                var strength = Mathf.Min(speed * d.magnitude / 0.8f, speed);
                return d.normalized * strength;
        }

        void BuildTeam(float radius) {
                float unit = 360.0f / agentCount;
                float degree = 0.0f;
                for (int i = 0; i < agentCount; i++) {
                        float rad = Mathf.Deg2Rad * degree;
                        float x = Mathf.Cos(rad) * radius;
                        float y = Mathf.Sin(rad) * radius;
                        teamSlot.Add(new Vector3(x, y, 0) + target);
                        degree += unit;
                };
        }

        // Update is called once per frame
        void FixedUpdate()
        {
                const float radius = 1.0f;
                foreach (var agent in pools) {
                        Plan(agent);
                        if (agent.path.Count == 0)
                                continue;
                        //TODO: check colider of wall
                        if (Colidering(agent, out Vector3 v)) {
                                agent.transform.position += v * Time.deltaTime;
                                continue;
                        }
                        var sep = Seperation(agent);
                        var coh = Cohersion(agent);
                        if (agent.isstable)
                                coh = Vector3.zero;
                        var p = agent.transform.position;
                        var t = agent.path[0];
                        var velocity = Vector3.zero;
                        var dir = (t - p);
                        if (dir.magnitude > Mathf.Epsilon)
                                velocity = dir.normalized * 0.5f;
                        velocity += sep + coh;
                        if (velocity.magnitude > 0.5f)
                                velocity = velocity.normalized * 0.3f;
                        var np = p + velocity * Time.deltaTime;
                        agent.transform.position = np;
                        if (agent.isstable == false && (np - target).magnitude < radius) {
                                if (teamSlot.Count == 0) {
                                        BuildTeam(1.0f);
                                }
                                agent.target = teamSlot[teamSlot.Count - 1];
                                agent.isstable = true;
                                teamSlot.RemoveAt(teamSlot.Count -1);
                                Debug.Log("Remove Left:" + teamSlot.Count + ":" + agent.transform.name + ":" + agent.target);
                        }
                }
        }
}
