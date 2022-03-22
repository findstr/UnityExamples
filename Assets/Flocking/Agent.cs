using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
        public float speed = 0.1f;
        public Vector3 Velocity = Vector3.zero;
        private Node current = null;
        private PathFinder path;
        public void Born(PathFinder pf, Node n) {
                path = pf;
                current = n;
                n.Enter(this.gameObject);
        }
        public void Moving()
        {
                if (current == null)
                        return ;
                var next = path.Next(current);
                if (next == null)
                        return ;
                Vector3 p = path.GridPosition(next.coord);
                p.z = transform.position.z;
                Velocity = (p - transform.position).normalized * speed;
                var npos = transform.position + Velocity * Time.deltaTime;
                if (Vector3.Distance(p, npos) < 0.1)
                        current = next;
                transform.position = npos;
        }
}
