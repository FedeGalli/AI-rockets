using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SphereLandingAgent : Agent
{

    private Rocket rocket;
    private float currentDistance;
    private float previousDistance;
    private float baseReward;
    private bool inside;
    private Vector3 startPosition;
    private Vector3 startRotation;
    private Vector3 startUpDirection;
    public GameObject target;
    public override void Initialize()
    {
        rocket = this.GetComponent<Rocket>();
        baseReward = 1f/MaxStep;
    }

    public override void OnEpisodeBegin()
    {
        rocket.GetComponent<Rigidbody>().freezeRotation=true;
        this.randomRelocateOnPlanet(0f, 30f, 0f);

        rocket.setIgnite(true);
        inside=false;
        previousDistance = currentDistance = Mathf.Abs((rocket.transform.position - target.transform.position).magnitude);
        startPosition = rocket.transform.position;
        startRotation = getBaseRotation().eulerAngles;
        startUpDirection = new Vector3(transform.up.x, transform.up.y + 1, transform.up.z);
        rocket.setTriggerLegsDeploy(true);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 rotation = startRotation - transform.rotation.eulerAngles;

        sensor.AddObservation(rocket.getAltitude());
        sensor.AddObservation(transform.InverseTransformDirection(target.transform.position -rocket.transform.position));
        sensor.AddObservation(startPosition - rocket.transform.position);
        sensor.AddObservation(rotation.x);
        sensor.AddObservation(rotation.z);
        sensor.AddObservation(transform.InverseTransformDirection(rocket.getRocketSpeed()));
        sensor.AddObservation(transform.InverseTransformDirection(rocket.getRocketAngularSpeed()));
        sensor.AddObservation(startUpDirection - transform.up);
        sensor.AddObservation(rocket.getRocketMass());
        sensor.AddObservation(transform.InverseTransformDirection(rocket.getEngineForce() - rocket.getRocketForce()));
        sensor.AddObservation(inside);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        rocket.setEngineThrust(actions.DiscreteActions[0]);
        rocket.setEngineX(actions.DiscreteActions[1]);
        rocket.setEngineZ(actions.DiscreteActions[2]);
        currentDistance = Mathf.Abs((rocket.transform.position - target.transform.position).magnitude);

        if (rocket.getAltitude() > 1f){
            rocket.GetComponent<Rigidbody>().freezeRotation=false;
        }
        if (rocket.getIsExploded()|| currentDistance > 40f) {
            SetReward(-1);
            EndEpisode();
        }

        if (currentDistance < previousDistance && !rocket.getIsLanded()) {
            AddReward(baseReward);
        }else{
            AddReward(-baseReward);
        }

        previousDistance = currentDistance;
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
            AddReward(0.1f);
    }

    public Rocket getRocket(){
        return rocket;
    }
    public void randomRelocateOnPlanet(float rocketSpawnRandomness, float rocketSpawnAltitude, float rocketRotationRandomness) {
        rocket.restart();
        Vector3 randomPoint = Random.onUnitSphere;
        Vector3 randomStartingPoint = new Vector3(randomPoint.x+ Random.Range(-rocketSpawnRandomness,rocketSpawnRandomness), randomPoint.y+ Random.Range(-rocketSpawnRandomness,rocketSpawnRandomness),  randomPoint.z+ Random.Range(-rocketSpawnRandomness,rocketSpawnRandomness)) * (rocket.startingPlanet.radius + rocketSpawnAltitude);
        //Vector3 randomStartingPoint = new Vector3(randomPoint.x, randomPoint.y,  randomPoint.z) * (startingPlanet.radius + 50f);

        this.transform.position = rocket.startingPlanet.transform.position + randomStartingPoint;
        this.transform.rotation = getBaseRotation();
        this.transform.rotation = Quaternion.Euler(this.transform.rotation.eulerAngles.x + Random.Range(-rocketRotationRandomness,rocketRotationRandomness), this.transform.rotation.eulerAngles.y + Random.Range(-rocketRotationRandomness,rocketRotationRandomness), this.transform.rotation.eulerAngles.z + Random.Range(-rocketRotationRandomness,rocketRotationRandomness));
        target.transform.position = rocket.startingPlanet.transform.position + new Vector3(randomPoint.x, randomPoint.y,  randomPoint.z) * (rocket.startingPlanet.radius - 1.7f);
    }
    public Quaternion getBaseRotation() {
        //RaycastHit hit;
        Vector3 normal = (rocket.startingPlanet.transform.position - this.transform.position).normalized;

        return Quaternion.FromToRotation (new Vector3(0, -1, 0), normal);
    }  
}
