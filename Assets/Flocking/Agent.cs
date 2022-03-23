using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

struct Conf {
        public const float maxSpeed = 0.5f;
        public const float maxAcceleation = 1.0f;
        public const float reach = 0.1f;
        public const float colidingRadius = 0.2f;
        public const float closeRadius = 0.3f;
        public const float cohesionRadius = 0.5f;
};

struct Seek {
        private GameObject character;
        private Vector3 target;
        private float acceleation;
        public Seek(GameObject c, Vector3 t, float s = Conf.maxSpeed) {
                character = c;
                target = t;
                acceleation = s;      
        }
        public Vector3 getSteering() {
                var dir = target - character.transform.position;
                return dir.normalized * acceleation;
        }
};

struct Seperation {
        private Agent character;
        private List<Agent> targets;
        public Seperation(Agent c, List<Agent> t) {
                character = c;
                targets = t;
        }
        public Vector3 getSteering() {
                int count = 0;
                Vector3 linear = Vector3.zero;
                Vector3 position = character.transform.position;
                foreach (var target in targets) {
                        var dir = position - target.transform.position;
                        var dist = Mathf.Max(dir.magnitude, Mathf.Epsilon);
                        if (dist > Conf.closeRadius || target == character)
                                continue;
                        count++;
                        var strength = Mathf.Min(2.0f * Conf.maxAcceleation * Conf.colidingRadius / dist, 10.0f * Conf.maxAcceleation);
                        linear += strength * dir.normalized;
                }
                if (count == 0)
                        return Vector3.zero;
                return linear / count;
        }
}

struct Cohesion {
        private Agent character;
        private List<Agent> targets;
        private const float maxThreshold = Conf.closeRadius;
        private const float minThreshold = Conf.colidingRadius;
        public Cohesion(Agent c, List<Agent> t) {
                character = c;
                targets = t;
        }
        public Vector3 getSteering() {
                int n = 0;
                Vector3 pos = Vector3.zero;
                Vector3 position = character.transform.position;
                foreach (var target in targets) {
                        var dir = position - target.transform.position;
                        var dist = Mathf.Max(dir.sqrMagnitude, Mathf.Epsilon);
                        if (dist > maxThreshold * maxThreshold)
                                continue;
                        ++n;
                        pos += target.transform.position;
                }
                if (n == 0)
                        return Vector3.zero;
                pos /= n;
                if ((pos - position).magnitude < Conf.closeRadius)
                        return Vector3.zero;
                Seek seek = new Seek(character.gameObject, pos, Conf.maxAcceleation);
                return seek.getSteering();
        }
};

struct Alignment {
        private Agent character;
        private List<Agent> targets;
        public Alignment(Agent c, List<Agent> t) {
                character = c;
                targets = t;
        }
        public Vector3 getSteering() {
                Vector3 v = Vector3.zero;
                if (targets.Count == 0)
                        return v;
                foreach (var target in targets) 
                        v += target.Velocity;
                return v / targets.Count;
        }
}

public class Agent : MonoBehaviour
{
        public Vector3 Velocity = Vector3.zero;
        public Vector3 v1 = Vector3.zero;
        public Vector3 v2 = Vector3.zero;
        public Vector3 v3 = Vector3.zero;
        public Vector3 v4 = Vector3.zero;
        public Vector3 target = Vector3.zero;
        private PathFinder path;
        public void Born(PathFinder pf) {
                path = pf;
                target = transform.position;
        }
        public int Moving(List<Agent> other)
        {
                path.Next(transform.position, out target);
                target.z = transform.position.z;
		var seek = new Seek(gameObject, target);
		var seperate = new Seperation(this, other);
                var cohesion = new Cohesion(this, other);
                var alignment = new Alignment(this, other);
		v1 = seek.getSteering();
		v2 = seperate.getSteering();
                v3 = cohesion.getSteering();
                //var v4 = alignment.getSteering();
                Velocity = v1 + v2 + v3;
                if (Velocity.magnitude > Conf.maxSpeed)
                        Velocity = Velocity.normalized * Conf.maxSpeed;
                else if (Velocity.magnitude < 0.001f) 
                        return 0;
                var npos = transform.position + Velocity * Time.deltaTime;
                transform.position = npos;
                return 1;
        }
}


[CustomEditor(typeof(Agent))]
class AgentEx: Editor {
	void OnSceneGUI()
	{
	        var self = target as Agent;
                Handles.DrawWireArc(self.transform.position, Vector3.forward, Vector3.left, 360.0f, Conf.colidingRadius);
                Handles.DrawWireArc(self.transform.position, Vector3.forward, Vector3.left, 360.0f, Conf.closeRadius);
        }
}

