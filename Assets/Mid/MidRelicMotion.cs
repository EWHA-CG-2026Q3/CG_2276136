using UnityEngine;

public sealed class MidRelicMotion : MonoBehaviour
{
    [System.Serializable]
    public struct Orbit
    {
        public Transform axis;
        public float degreesPerSecond;
    }

    [SerializeField] private Orbit[] orbits;
    [SerializeField] private Transform stone;

    private Vector3 restPosition;
    private Quaternion stoneTilt;
    private Quaternion[] orbitTilts;
    private Quaternion[] shardTilts;

    public void Bind(Orbit[] shardOrbits, Transform sealStone)
    {
        orbits = shardOrbits;
        stone = sealStone;
    }

    private void Awake()
    {
        RememberPose();
    }

    private void Update()
    {
        ApplyPose(Time.timeSinceLevelLoad);
    }

    public void RememberPose()
    {
        restPosition = transform.localPosition;
        stoneTilt = stone.localRotation;
        orbitTilts = new Quaternion[orbits.Length];
        shardTilts = new Quaternion[orbits.Length];
        for (int i = 0; i < orbits.Length; i++)
        {
            orbitTilts[i] = orbits[i].axis.localRotation;
            if (orbits[i].axis.childCount > 0)
                shardTilts[i] = orbits[i].axis.GetChild(0).localRotation;
        }
    }

    public void ApplyPose(float seconds)
    {
        transform.localPosition = restPosition + Vector3.up * (0.12f * Mathf.Sin(seconds * 1.25f));
        stone.localRotation = Quaternion.AngleAxis(seconds * -12f, Vector3.up) * stoneTilt;
        for (int i = 0; i < orbits.Length; i++)
        {
            var orbit = orbits[i];
            float angle = seconds * orbit.degreesPerSecond;
            orbit.axis.localRotation = orbitTilts[i] * Quaternion.AngleAxis(angle, Vector3.up);
            if (orbit.axis.childCount > 0)
            {
                var shard = orbit.axis.GetChild(0);
                shard.localRotation = shardTilts[i] *
                    Quaternion.AngleAxis(seconds * (18f + i * 4f), new Vector3(0.35f, 1f, 0.2f).normalized);
            }
        }
    }
}

