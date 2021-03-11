using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WayPointCollider : MonoBehaviour
{
    private GameObject core;
    public GameObject cameraEye;
    public GameManager gameManager;
    public bool trialEnd;
    public bool blueWaypoint;
    private Vector3 currentplayerPosition;
    private Vector3 previousplayerPosition;
    private float shortestDistance;
    private float timeDuringShortestDistance;
    private float lx1, lx2, ly1, ly2; 
    [SerializeField]
    private HyperJump hyperJump;
    public bool lastWaypoint;
    public AudioSource audioSource;
    public AudioClip passWaypointSound;
    public AudioClip tooFar;
    public AudioClip stopSound;
    private string wayPointName;
    
    void Start()
    {
        core = this.gameObject.transform.GetChild(0).gameObject;
        currentplayerPosition = cameraEye.transform.position;
        previousplayerPosition = currentplayerPosition;
        shortestDistance = Mathf.Infinity;
        lx1 = core.transform.position.x + 10 * core.transform.forward.x;
        lx2 = core.transform.position.x - 10 * core.transform.forward.x;
        ly1 = core.transform.position.z + 10 * core.transform.forward.z;
        ly2 = core.transform.position.z - 10 * core.transform.forward.z;
        wayPointName = this.gameObject.ToString();
    }
    void Update()
    {
        // measuring the shortest distance to the core
        // Player travel line
        currentplayerPosition = cameraEye.transform.position;
        float x1 = currentplayerPosition.x;
        float y1 = currentplayerPosition.z;
        float x2 = previousplayerPosition.x;
        float y2 = previousplayerPosition.z;
        // Core point
        float x0 = core.transform.position.x;
        float y0 = core.transform.position.z;
        Vector2 p1 = new Vector2(x1, y1);
        Vector2 q1 = new Vector2(x2, y2);
        Vector2 target = new Vector2 (x0, y0);
    
        // https://stackoverflow.com/questions/849211/shortest-distance-between-a-point-and-a-line-segment
        // second answer
    
        float distance = distanceBetweenPointandLine(p1, q1, target);
    
        // update the distance to the shortest point
        if (distance < shortestDistance)
        {
            shortestDistance = distance;
            timeDuringShortestDistance = Time.time;
        }

        // measuring if the player has crossed the waypoint plane
        // player line segement p1(x1, y1), q1(x2, y2) && waypoint line segment p2(lx1, ly1), q2(lx2, ly2)
        // https://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/
        Vector2 p2 = new Vector2(lx1, ly1);
        Vector2 q2 = new Vector2(lx2, ly2);
        if(!lastWaypoint)
        {
            if(intersectTwoLines(p1, q1, p2, q2))
            {
                if (trialEnd)
                {
                    Application.Quit();
                }
                if (shortestDistance < .25f)
                {
                    audioSource.clip = passWaypointSound;
                }
                else
                {
                    audioSource.clip = tooFar;
                }
                audioSource.Play();
                gameManager.updateWayPointInfo(blueWaypoint, shortestDistance, timeDuringShortestDistance, core.transform.position, cameraEye.transform.position, wayPointName);
                gameManager.writeWayPointData = true;
                // Debug.Log(timeDuringShortestDistance);
                this.gameObject.SetActive(false);
            }
        }
        else
        {
            lx1 = core.transform.position.x + 1.25f * core.transform.forward.x;
            lx2 = core.transform.position.x - 1.25f * core.transform.forward.x;
            ly1 = core.transform.position.z + 1.25f * core.transform.forward.z;
            ly2 = core.transform.position.z - 1.25f * core.transform.forward.z;

            p2 = new Vector2(lx1, ly1);
            q2 = new Vector2(lx2, ly2);

            if(intersectTwoLines(p1, q1, p2, q2))
            {                
                hyperJump.SetBreak(true);
                audioSource.Play();
                gameManager.updateWayPointInfo(blueWaypoint, shortestDistance, timeDuringShortestDistance, core.transform.position, cameraEye.transform.position, wayPointName);
                gameManager.writeWayPointData = true;
                gameManager.setPointingTask(true);
                audioSource.clip = stopSound;
                audioSource.Play();
                this.gameObject.SetActive(false);
            }
        }
       
        
        previousplayerPosition = currentplayerPosition;
    }
   
    private bool intersectTwoLines(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
    {
        // find the four orientations needed 
        int o1 = orientation(p1, q1, p2);
        int o2 = orientation(p1, q1, q2);
        int o3 = orientation(p2, q2, p1);
        int o4 = orientation(p2, q2, q1);

        // General Case
        if (o1 != o2 && o3!= o4) return true;
        
        // Special Cases 
        // p1, q1 and p2 are colinear and p2 lies on segment p1q1 
        if (o1 == 0 && onSegment(p1, p2, q1)) return true; 
    
        // p1, q1 and q2 are colinear and q2 lies on segment p1q1 
        if (o2 == 0 && onSegment(p1, q2, q1)) return true; 
    
        // p2, q2 and p1 are colinear and p1 lies on segment p2q2 
        if (o3 == 0 && onSegment(p2, p1, q2)) return true; 
    
        // p2, q2 and q1 are colinear and q1 lies on segment p2q2 
        if (o4 == 0 && onSegment(p2, q1, q2)) return true; 
    
        // Doesn't meet any of the above conditions
        return false;
    }

    private bool onSegment (Vector2 p, Vector2 q, Vector2 r)
    {
       if (q.x <= Mathf.Max(p.x, r.x) && q.x >= Mathf.Min(p.x, r.x) && 
           q.y <= Mathf.Max(p.y, r.y) && q.y >= Mathf.Min(p.y, r.y)) 
        return true; 
        
        return false;
    }

    private int orientation (Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
        if (val == 0) return 0; // colinear
        return (val > 0)? 1 : 2; // clock or counterclock
    }
    private float distanceBetweenPointandLine(Vector2 p1, Vector2 q1, Vector2 target)
    {
        // Calculate the projection point of the core on the line segment
        float a = target.x - p1.x;
        float b = target.y - p1.y;
        float c = q1.x - p1.x;
        float d = q1.y - p1.y;
        float dot = a * c + b * d;
        float len_sq = c * c + d * d;
        float param = -1;
        if (len_sq != 0) param = dot / len_sq;
        float xx, yy;

        
        if (param < 0) // if the project lies outside of the line segment in one direction
        {
            xx = p1.x;
            yy = p1.y;
        }
        else if (param > 1) // if the project lies outside of the line segment in the other direction
        {
            xx = q1.x;
            yy = q1.y;
        }
        else  // if the project lies inside the line
        {
            xx = p1.x + param * c;
            yy = p1.y + param * d;
        }

        float dx = target.x - xx;
        float dy = target.y - yy;

        return Mathf.Sqrt(dx * dx + dy * dy);
    }
}
