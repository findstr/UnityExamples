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
                var p = path.GridPosition(next.coord);
                Velocity = (new Vector3(p.x, p.y, 0) - transform.position).normalized * speed;
                var npos = transform.position + Velocity * Time.deltaTime;
                var nnode = path.WhichGridNode(npos);
                //Debug.Log("1:current:" + current.coord + ":node:" + nnode.coord + "next:" + next.coord + ":V:" + Velocity);
                //Debug.Log("2:current:" + path.GridPosition(current.coord) + ":node:" + path.GridPosition(nnode.coord) + "next:" + path.GridPosition(next.coord) + ":V:" + Velocity);
                //Debug.Log("3:current:" + transform.position + ":node:" + path.GridPosition(nnode.coord) + "next:" + path.GridPosition(next.coord) + ":V:" + Velocity);
                if (nnode == next) { 
                        Debug.Log("Switch:" + next.coord + ":current:" + current.coord + ":node:" + nnode.coord);
                        current = next;
                }
                transform.position = npos;
        }
}
