using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Node {
	public int close = 0;
	public Vector3Int coord;
	public Node next = null;
	HashSet<GameObject> inside = new HashSet<GameObject>();
	public bool CanEnter() {
		return inside.Count >= 5;
	}
	public void Enter(GameObject go) {
		inside.Add(go);
	}
	public void Leave(GameObject go) {
		inside.Remove(go);
	}
};

public class OpenList {
	private Dictionary<Node, int> score = new Dictionary<Node, int>();
	public bool Pop(out Node n, out int cost) {
		n = null;
		cost = 99999999;
		if (score.Count == 0)
			return false;
		foreach (var iter in score) {
			if (iter.Value < cost) {
				cost = iter.Value;
				n = iter.Key;
			}
		}
		score.Remove(n);
		return true;
	}
	public bool Push(Node n, int cost) {
		if (score.TryGetValue(n, out int s) && s <= cost) 
			return false;
		score[n] = cost;
		return true;
	}
	public bool IsEmpty() {
		return score.Count == 0;
	}
}


public class PathFinder : MonoBehaviour
{
	[SerializeField]
	public float cellSize = 0.1f;
	public LayerMask ObstacleLayer;
	public Node[,] grids;
	public Vector2Int grid_range = Vector2Int.zero;
	public Vector2Int goal = Vector2Int.zero;
	private Vector3 target_position = Vector3.zero;
	private OpenList open = new OpenList();
	public int close_idx = 1;
	// Start is called before the first frame update
	void Awake()
	{
		float xsize = transform.localScale.x;
		float ysize = transform.localScale.y;
		int xn = (int)(xsize / cellSize);
		int yn = (int)(ysize / cellSize);
		grid_range = new Vector2Int(xn, yn);
		grids = new Node[yn, xn];
		for (int y = 0; y < yn; y++) {
			for (int x = 0; x < xn; x++) {
				Vector2 pos = GridPosition(x, y);
				if (!Physics2D.OverlapBox(pos, new Vector2(cellSize, cellSize), 0, ObstacleLayer)) {
					grids[y,x] = new Node() {
						coord = new Vector3Int(x, y, 0),
					};
				} else {
					grids[y,x] = null;
				}
			}
		}
		Debug.Log("xn:" + xn + " yn:" + yn);
	}
	public bool Next(Vector3 current, out Vector3 target) {
                var n = WhichGridNode(current);
		if (n != null && n.next != null && n.next.close == close_idx) {
			target = GridPosition(n.next.coord);
			return false;
		}
		target = target_position;
		return true;
	}
	public Vector3 Around(Vector3 current) {
		int n = 1;
		Vector2Int c = WhichGrid(current);
		Vector3 pos = GridPosition(c.x, c.y);
		for (int i = 0; i < around.Length; i++) {
			var a = around[i];
			var coord = new Vector2Int(a.x, a.y) + c;
			if (coord.x >= 0 && coord.x < grid_range.x && coord.y >=0 && coord.y < grid_range.y) {
				var g = grids[coord.y, coord.x];
				if (g != null) {
					var p = GridPosition(coord.x, coord.y);
					pos += new Vector3(p.x, p.y, 0);
					++n;
				}
			}
		}
		return pos / n;
	}
	public Vector2 GridPosition(int x, int y) {
		if (goal.x == x && goal.y == y)
			return target_position;
		else
			return new Vector2(x * cellSize, y * cellSize) - new Vector2(transform.localScale.x, transform.localScale.y) / 2 + new Vector2(cellSize / 2, cellSize / 2);
	}
	public Vector3 GridPosition(Vector3Int v) {
		var p = GridPosition(v.x, v.y);
		return new Vector3(p.x, p.y, 0);
	}
	public Vector2Int WhichGrid(Vector3 pos) {
		Vector2Int coord = new Vector2Int();
		pos = pos - transform.position + transform.localScale / 2;
		coord.x = (int)(pos.x / cellSize);
		coord.y = (int)(pos.y / cellSize);
		return coord;
	}
	public Node WhichGridNode(Vector3 pos) {
		Vector2Int coord = WhichGrid(pos);
		return grids[coord.y, coord.x];
	}
	public Node GetNode(Vector2Int coord) {
		return grids[coord.y, coord.x];
	}

	public Vector3Int[] around = new Vector3Int[] {
		new Vector3Int(-1, -1, 15), new Vector3Int(0, -1, 10), new Vector3Int(1, -1, 15),
		new Vector3Int(-1,  0, 10),				new Vector3Int(1, 0, 10),
		new Vector3Int(-1,  1, 15), new Vector3Int(0, 1, 10), new Vector3Int(1, 1, 15),
	};

	public void Bake(Vector3 point) {
		goal = WhichGrid(point);
		var target = grids[goal.y, goal.x];
		if (target == null)
			return ;
		target_position = point;
		open.Push(target, 0);
		target.next = null;
		while (!open.IsEmpty()) {
			open.Pop(out Node p, out int cost);
			p.close = close_idx;
			for (int i = 0; i < around.Length; i++) {
				var x = around[i];
				var coord = new Vector3Int(x.x, x.y, 0) + p.coord;
				if (coord.x >= 0 && coord.x < grid_range.x && coord.y >=0 && coord.y < grid_range.y) {
					var n = grids[coord.y, coord.x];
					if (n != null && n.close != close_idx && open.Push(n, cost + x.z))
						n.next = p;
				}
			}
		}
		Debug.Log("Bake:" + target.next);
	}
	// Update is called once per frame
	void Update()
	{
		if (Input.GetMouseButtonDown(0)) {
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(ray, out RaycastHit hit, 30)) {
				++close_idx;
				Bake(hit.point);
				Debug.Log("Goal:" + goal + ":" + hit.point);
			}
		}
	}
}


[CustomEditor(typeof(PathFinder))]
class PathFinderEx : Editor {
	void OnSceneGUI()
	{
	        var self = target as PathFinder;
                if (self.grids == null)
                        return ;
		for (int y = 0; y < self.grid_range.y; y++) {
			for (int x = 0; x < self.grid_range.x; x++) {
				var n = self.grids[y,x];
				if (n == null) 
					continue;
				var p1 = self.GridPosition(x, y);
				var size = new Vector2(self.cellSize, self.cellSize) / 2.0f;
				Handles.Label(p1 - new Vector2(self.cellSize / 2, 0), string.Format("x:{0} y:{1}", x, y));
				Handles.DrawLine(p1 + size * new Vector2(-1,-1), p1 + size * new Vector2(1, -1));
				Handles.DrawLine(p1 + size * new Vector2(-1,1), p1 + size * new Vector2(1, 1));
				Handles.DrawLine(p1 + size * new Vector2(-1,-1), p1 + size * new Vector2(-1, 1));
				Handles.DrawLine(p1 + size * new Vector2(1,-1), p1 + size * new Vector2(1, 1));
				if (n.next != null && n.next.close == self.close_idx) {
					var p2 = self.GridPosition(n.next.coord.x, n.next.coord.y);
					Handles.DrawLine(p1, p2);
				}
			}
		}
		Rect rt = new Rect {
			center = self.GridPosition(self.goal.x, self.goal.y) - new Vector2(self.cellSize / 2, self.cellSize / 2),
			size = new Vector2(self.cellSize, self.cellSize)
		};
		Handles.DrawSolidRectangleWithOutline(rt, Color.red, Color.red);
	}
}
