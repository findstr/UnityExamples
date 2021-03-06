using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flocking : MonoBehaviour
{
        public PathFinder PathFinder;
        public GameObject templ;
        public Flocking enemy;
        public int click = 0;
        public int agentCount = 0;
        public float seperationWeight = 1.0f;
        public float colliderWeight = 10.0f;
        public float coherisonWeight = 0.5f;
        public float alighWeight = 0.5f;        
        public Vector3 target = Vector3.zero;
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
                        pools.Add(go.GetComponent<Agent>());
                        PathFinder.Enter(pos, go);
                }
        }

        // Update is called once per frame
	void Update()
	{
		if (Input.GetMouseButtonDown(click)) {
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit, 30)) {
                                target = hit.point;
                                PathFinder.Bake(target);
                                foreach (var agent in pools) 
                                        agent.isstable = false;
                                Debug.Log("Bake");
			}
		}
	}
        public bool Colidering(Agent character, out Vector3 v) {
                float speed = character.speed;
                v = Vector3.zero;
                var cdir = PathFinder.Collider(character.transform.position,  character.colliderRadius);
                if (cdir.magnitude > Mathf.Epsilon) {
                        v += cdir * speed;
                        return true;
                }
                return false;
        }
        public int enter = 0;
	public Vector3 Seperation(Agent character) {
                if (enter == 1)
                        return Vector3.zero;
                enter = 1;
                int n = 0;
                float min_radius = 0.0f;
                float max_radius = character.closeRadius;
                float speed = character.speed;
                Vector3 v = Vector3.zero;
                foreach (var t in pools) {
                        if (t == character)
                                continue;
                        var dir =  character.transform.position - t.transform.position;
                        var dist = dir.magnitude;
                        if (dist > min_radius && dist <= max_radius) {
                                ++n;
                                var strength = Mathf.Min(speed * character.closeRadius / dist, speed * 20.0f);
                                v += dir.normalized * strength;
                        }
                }
                if (enemy != null) {
                        ++n;
                        v += enemy.Seperation(character);
                }
                if (n > 0)
                        v /= n;
                enter = 0;
                return v;
        }

        Vector3 Cohersion(Agent character) {
                int n = 0;
                Vector3 center= Vector3.zero;
                float radius = character.cohesionRadius;
                float speed = character.speed;
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
                if (d.magnitude <= character.closeRadius)
                        return Vector3.zero;
                var strength = Mathf.Min(speed * d.magnitude / 0.8f, speed);
                return d.normalized * strength;
        }

        Vector3 Alignment(Agent character) {
                int n = 0;
                Vector3 v = Vector3.zero;
                float speed = character.speed;
                foreach (var t in pools) {
                        var dir = character.transform.position - t.transform.position;
                        if (dir.magnitude <= character.cohesionRadius) {
                                ++n;
                                v += t.Velocity;
                        }
                }
                if (n == 0)
                        return Vector3.zero;
                v /= n;
                return v;
        }	

        void BuildTeam(float radius) {
                teamSlot.Clear();
                float unit = 360.0f / agentCount;
                float degree = 0.0f;
                for (int i = 0; i < agentCount; i++) {
                        float rad = Mathf.Deg2Rad * degree;
                        float x = Mathf.Cos(rad) * radius;
                        float y = Mathf.Sin(rad) * radius;
                        var pos = new Vector3(x, y, 0) + target;
                        pos.z = 0.0f;
                        teamSlot.Add(pos);
                        degree += unit;
                };
        }

        List<Vector3> path = new List<Vector3>();
        // Update is called once per frame
        void FixedUpdate()
        {
                foreach (var agent in pools) {
                        Colidering(agent, out Vector3 colliderv);
                        if (!PathFinder.Next(agent.transform.position, out Vector3 t))
                                continue;
                        if (agent.isstable)
                                t = agent.target;
                        t.z = agent.transform.position.z;
                        var sep = Seperation(agent);
                        var coh = Cohersion(agent);
                        var align = Alignment(agent);
                        if (agent.isstable) {
                                align = Vector3.zero;
                                coh = Vector3.zero;
                        }
                        var p = agent.transform.position;
                        var velocity = Vector3.zero;
                        var direction = (t - p);
                        if (direction.magnitude > 0.1f) {
                                agent.Isrunning = true;
                                velocity = direction.normalized * agent.speed;
                        } else {
                                agent.Isrunning = false;
                        }
                        velocity += colliderWeight * colliderv + seperationWeight * sep + coherisonWeight * coh + alighWeight * align;
                        if (velocity.magnitude > agent.speed)
                                velocity = velocity.normalized * agent.speed;
                        var np = p + velocity * Time.deltaTime;
                        agent.transform.position = np;
                        /*
                        if (agent.isstable == false && (np - target).magnitude < 0.5f) {
                                if (teamSlot.Count == 0)
                                        BuildTeam(1.0f);
                                agent.isstable = true;
                                agent.target = teamSlot[teamSlot.Count - 1];
                                teamSlot.RemoveAt(teamSlot.Count -1);
                                Debug.Log("Target:" + agent.name + ":" + agent.target);
                        }*/
                        agent.Velocity = velocity;
                }
        }


}
