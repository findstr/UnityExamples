using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Conf {
        public const float maxAcceleration = 10f;
        public const float maxSpeed = 0.5f;
        public const float reach = 0.1f;
        public const float threshold = 0.2f;
        public const float decayCoefficient = 0.1f;
};

struct Seek {
        private GameObject character;
        private Vector3 target;
        public Seek(GameObject c, Vector3 t) {
                character = c;
                target = t;
        }
        public Vector3 getSteering() {
                var dir = target - character.transform.position;
                return dir.normalized * Conf.maxSpeed;
        }
};

struct Seperation {
        private PathFinder path;
        private Agent character;
        private List<Agent> targets;
        public Seperation(PathFinder pf, Agent c, List<Agent> t) {
                path = pf;
                character = c;
                targets = t;
        }
        public Vector3 getSteering() {
                int count = 0;
                Vector3 linear = Vector3.zero;
                Vector3 position = character.transform.position + character.Velocity * Time.deltaTime;
                foreach (var target in targets) {
                        var dir = position - target.transform.position;
                        var dist = Mathf.Max(dir.sqrMagnitude, Mathf.Epsilon);
                        if (dist > (Conf.threshold * Conf.threshold) || target == character)
                                continue;
                        count++;
                        var strength = Mathf.Min(Conf.decayCoefficient / dist, Conf.maxAcceleration);
                        linear += strength * dir.normalized;
                }
                /*
                Vector3 p2 = path.Around(position);
                var odir = p2 - position;
                var odist = Mathf.Max(odir.sqrMagnitude, Mathf.Epsilon);
                if (odist < (Conf.threshold * Conf.threshold)) {
                        ++count;
                        var strength = Mathf.Min(Conf.decayCoefficient / odist, Conf.maxAcceleration);
                        linear += strength * odir.normalized;
                }*/
                if (count == 0)
                        return Vector3.zero;
                return linear / count;
        }
}

public class Agent : MonoBehaviour
{
        public const float speed = 0.5f;
        public Vector3 Velocity = Vector3.zero;
        private PathFinder path;
        private Vector3 target = Vector3.zero;
        public void Born(PathFinder pf) {
                path = pf;
                target = transform.position;
        }
        public int Moving(List<Agent> other)
        {
                if ((target - transform.position).sqrMagnitude < Conf.reach) {
                        path.Next(transform.position, out Vector3 p);
		        p.z = transform.position.z;
                        if ((p - transform.position).magnitude < Conf.reach)
                                return 0;
                        target = p;
                }
		var seperate = new Seperation(path, this, other);
		var v2 = seperate.getSteering();
		var seek = new Seek(gameObject, target);
		var v1 = seek.getSteering();
                Velocity = v1 + 1.5f * v2;
                if (Velocity.magnitude > speed)
                        Velocity = Velocity.normalized * speed;
                transform.position = transform.position + Velocity * Time.deltaTime;
                return 1;
        }
}
