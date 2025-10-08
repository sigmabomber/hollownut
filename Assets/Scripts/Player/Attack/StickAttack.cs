using UnityEngine;

public class StickAttack : MonoBehaviour
{
    void Start()
    {
        CreateSlashEffect();
    }

    void CreateSlashEffect()
    {
        // Get or add Particle System component
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps == null) ps = gameObject.AddComponent<ParticleSystem>();

        // Configure the particle system in code
        var main = ps.main;
        main.startSpeed = 0f;
        main.startLifetime = 0.5f;
        main.startSize = 0.3f;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;

        // Emission - burst all particles at start
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 50)
        });

        // Shape - use Circle with 180 degree arc for half-circle
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 2f;
        shape.arc = 180f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
        shape.arcSpread = 0f;

        // Make particles follow arc path using velocity
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;

        // Create arc motion - particles move along X axis with Y arc
        AnimationCurve curveX = new AnimationCurve(
            new Keyframe(0f, -3f),
            new Keyframe(0.3f, 0f),
            new Keyframe(1f, 3f)
        );
        AnimationCurve curveY = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 2f),
            new Keyframe(1f, 0f)
        );
        AnimationCurve curveZ = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(1f, 0f)
        );

        // Set all velocity components to use Curve mode
        velocity.x = new ParticleSystem.MinMaxCurve(4f, curveX);
        velocity.y = new ParticleSystem.MinMaxCurve(3f, curveY);
        velocity.z = new ParticleSystem.MinMaxCurve(1f, curveZ);

        // Color over lifetime - fade based on X position (left to right)
        var color = ps.colorOverLifetime;
        color.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),    // Left side - bright
                new GradientColorKey(Color.cyan, 0.3f),   // Middle
                new GradientColorKey(Color.blue, 0.6f),   // Right side
                new GradientColorKey(Color.black, 1f)     // End
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),    // Left side - fully visible
                new GradientAlphaKey(1f, 0.3f),  // Still visible in middle
                new GradientAlphaKey(0.5f, 0.6f), // Start fading on right side
                new GradientAlphaKey(0f, 1f)      // Completely transparent at end
            }
        );
        color.color = gradient;

        // Size over lifetime - fade along X axis
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.4f),  // Start medium size on left
            new Keyframe(0.5f, 0.6f), // Grow in middle
            new Keyframe(0.8f, 0.3f), // Shrink on right side
            new Keyframe(1f, 0f)      // Disappear at end
        );
        size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Rotation over lifetime
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(2f, 6f);

        // Renderer settings
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.material = CreateParticleMaterial();
        }
    }

    private Material CreateParticleMaterial()
    {
        Material material = new Material(Shader.Find("Particles/Standard Unlit"));
        material.color = Color.white;
        return material;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            // Clear and play the particle system
            ParticleSystem ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Clear();
                ps.Play();
            }
        }
    }
}