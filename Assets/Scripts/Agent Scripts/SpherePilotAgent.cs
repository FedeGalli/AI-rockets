using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SpherePilotAgent : Agent
{
    private Rocket rocket;
    private float currentDistance;
    private float previousDistance;
    private float baseReward;
    private bool inside;
    private Vector3 startPosition;
    private Vector3 startRotation;
    public GameObject target;

    public Transform tangentPlane;
    public override void Initialize()
    {
        rocket = this.GetComponent<Rocket>();
        baseReward = 1f/MaxStep;
        tangentPlane.position = new Vector3(0,0,0);
    }

    public override void OnEpisodeBegin()
    {
        
        this.randomRelocateOnPlanet(Academy.Instance.EnvironmentParameters.GetWithDefault("pilot_randomization", 2f));
        rocket.GetComponent<Rigidbody>().freezeRotation=true;    //SET ONLY ON THE FIRST PART OF THE TRAINING TO PREVENT THE ROCKET FALL
        rocket.setIgnite(true);
        inside=false;
        previousDistance = currentDistance = Mathf.Abs((rocket.transform.position - target.transform.position).magnitude);
        startPosition = rocket.transform.localPosition;
        startRotation = getBaseRotation().eulerAngles;
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        sensor.AddObservation(rocket.getAltitude());
        sensor.AddObservation(transform.InverseTransformDirection(target.transform.position -rocket.transform.position));
        sensor.AddObservation(transform.InverseTransformDirection(rocket.getRocketSpeed()));
        sensor.AddObservation(transform.InverseTransformDirection(rocket.getRocketAngularSpeed()));
        sensor.AddObservation(Vector3.Angle(rocket.transform.up, (tangentPlane.right)));
        sensor.AddObservation(Vector3.Angle(rocket.transform.up, (rocket.transform.position - rocket.startingPlanet.transform.position)));
        sensor.AddObservation(rocket.getRocketMass());
        sensor.AddObservation(transform.InverseTransformDirection(rocket.getEngineForce()));
        sensor.AddObservation(transform.InverseTransformDirection(rocket.getRocketForce()));
        sensor.AddObservation(inside);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        rocket.setEngineThrust(actions.DiscreteActions[0]);
        rocket.setEngineX(actions.DiscreteActions[1]);
        rocket.setEngineZ(actions.DiscreteActions[2]);
        currentDistance = Mathf.Abs((rocket.transform.position - target.transform.position).magnitude);

        if (rocket.getAltitude() > 1f){
            rocket.GetComponent<Rigidbody>().freezeRotation=false;    //SET ONLY ON THE FIRST PART OF THE TRAINING TO PREVENT THE ROCKET FALL
        }
        if (rocket.getIsExploded()|| currentDistance > 100f) {
            EndEpisode();
        }
 
        if (currentDistance < previousDistance && !rocket.getIsLanded()) {
                AddReward(baseReward);
        }else{
                AddReward(-0.75f * baseReward);
        }

        previousDistance = currentDistance;
        Debug.DrawRay(rocket.transform.position, rocket.getEngineForce() + rocket.getRocketForce(), Color.green, 0f);
        Debug.DrawRay(rocket.transform.position, rocket.getRocketForce(), Color.magenta, 0f);
        Debug.DrawRay(rocket.transform.position, rocket.getEngineForce(), Color.blue, 0f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Input.GetKey(KeyCode.UpArrow)){
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.DownArrow)){
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.W)){
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.S)){
            discreteActionsOut[1] = 2;
        }
        if (Input.GetKey(KeyCode.A)){
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.D)){
            discreteActionsOut[2] = 2;
        }
        if (Input.GetKey(KeyCode.P)){
            rocket.setTriggerLegsDeploy(true);
        }   
        
    }

    
    private void OnTriggerEnter(Collider collider){
        if (collider.gameObject.GetInstanceID() == target.gameObject.GetInstanceID())
            inside = true;
    }

    private void OnTriggerExit(Collider collider) {
        if (collider.gameObject.GetInstanceID() == target.gameObject.GetInstanceID())
            inside = false;
    }

    private void OnTriggerStay(Collider collider) {
        if (collider.gameObject.GetInstanceID() == target.gameObject.GetInstanceID())
            AddReward(baseReward * 5);
    }

    public Rocket getRocket(){
        return rocket;
    }
    public void randomRelocateOnPlanet(float targetSpawnRandomness) {
        rocket.restart();
        Vector3 randomPoint = Random.onUnitSphere;
        Vector3 randomStartingPoint = new Vector3(randomPoint.x, randomPoint.y,  randomPoint.z) * (rocket.startingPlanet.radius + 0.1f);
        this.transform.position = rocket.startingPlanet.transform.position + randomStartingPoint;
        this.transform.rotation = getBaseRotation();
        
        target.transform.position = rocket.startingPlanet.transform.position + new Vector3(randomPoint.x, randomPoint.y,  randomPoint.z) * (rocket.startingPlanet.radius + 20f + (targetSpawnRandomness * 10f));
        target.transform.position = target.transform.position + new Vector3(Random.Range(-targetSpawnRandomness,targetSpawnRandomness), Random.Range(-targetSpawnRandomness,targetSpawnRandomness),Random.Range(-targetSpawnRandomness,targetSpawnRandomness));
        target.transform.localScale = new Vector3(11 - targetSpawnRandomness, 11 -targetSpawnRandomness, 11-targetSpawnRandomness);
    }
    public Quaternion getBaseRotation() {
        //RaycastHit hit;
        Vector3 normal = (rocket.startingPlanet.transform.position - this.transform.position).normalized;

        return Quaternion.FromToRotation (new Vector3(0, -1, 0), normal);
    }

    void FixedUpdate(){
        tangentPlane.LookAt(rocket.transform, -Vector3.up);
    }
}
