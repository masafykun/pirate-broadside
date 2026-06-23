using System;
using System.Collections.Generic;
using UnityEngine;

namespace PirateBroadside
{
    public readonly struct WorldBuildResult
    {
        public WorldBuildResult(PlayerShip player, List<EnemyShip> enemies)
        {
            Player = player;
            Enemies = enemies;
        }

        public PlayerShip Player { get; }
        public List<EnemyShip> Enemies { get; }
    }

    public static class WorldBuilder
    {
        public static WorldBuildResult Build(Transform parent)
        {
            ConfigureRendering();
            CreateSun(parent);
            CreateOcean(parent);
            CreateIslands(parent);

            var player = ShipFactory.Create<PlayerShip>(
                "HMS Resolute", new Vector3(0f, 0f, -35f), Quaternion.Euler(0f, 8f, 0f), ShipTeam.Player);

            var enemies = new List<EnemyShip>
            {
                ShipFactory.Create<EnemyShip>("Crimson Fang", new Vector3(-58f, 0f, 28f), Quaternion.Euler(0f, 138f, 0f), ShipTeam.Pirate),
                ShipFactory.Create<EnemyShip>("Black Wake", new Vector3(62f, 0f, 48f), Quaternion.Euler(0f, 214f, 0f), ShipTeam.Pirate),
                ShipFactory.Create<EnemyShip>("Sea Viper", new Vector3(12f, 0f, 112f), Quaternion.Euler(0f, 186f, 0f), ShipTeam.Pirate)
            };

            player.transform.SetParent(parent, true);
            foreach (var enemy in enemies)
            {
                enemy.transform.SetParent(parent, true);
                enemy.SetTarget(player);
            }

            CreateCamera(parent, player);
            return new WorldBuildResult(player, enemies);
        }

        private static void ConfigureRendering()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 135f;
            RenderSettings.fogEndDistance = 470f;
            RenderSettings.fogColor = new Color(0.47f, 0.73f, 0.82f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.50f, 0.68f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.30f, 0.46f, 0.49f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.17f, 0.16f);

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetColor("_SkyTint", new Color(0.30f, 0.64f, 0.90f));
                sky.SetColor("_GroundColor", new Color(0.12f, 0.32f, 0.37f));
                sky.SetFloat("_AtmosphereThickness", 0.72f);
                sky.SetFloat("_SunSize", 0.045f);
                sky.SetFloat("_Exposure", 1.18f);
                RenderSettings.skybox = sky;
            }
        }

        private static void CreateSun(Transform parent)
        {
            var sunObject = new GameObject("Sun", typeof(Light));
            sunObject.transform.SetParent(parent, false);
            sunObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            var sun = sunObject.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.86f, 0.66f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.78f;
            RenderSettings.sun = sun;

            var fillObject = new GameObject("Sky Fill", typeof(Light));
            fillObject.transform.SetParent(parent, false);
            fillObject.transform.rotation = Quaternion.Euler(52f, 145f, 0f);
            var fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.36f, 0.58f, 0.78f);
            fill.intensity = 0.36f;
            fill.shadows = LightShadows.None;
        }

        private static void CreateOcean(Transform parent)
        {
            var ocean = new GameObject("Ocean", typeof(MeshFilter), typeof(MeshRenderer));
            ocean.transform.SetParent(parent, false);
            ocean.transform.position = new Vector3(0f, -0.58f, 0f);
            ocean.GetComponent<MeshFilter>().sharedMesh = CreateOceanMesh(720f, 72);
            ocean.GetComponent<MeshRenderer>().sharedMaterial = WorldMaterials.Ocean;
        }

        private static Mesh CreateOceanMesh(float size, int segments)
        {
            var mesh = new Mesh { name = "Ocean Grid" };
            var vertices = new Vector3[(segments + 1) * (segments + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * segments * 6];
            var step = size / segments;
            var index = 0;
            for (var z = 0; z <= segments; z++)
            {
                for (var x = 0; x <= segments; x++)
                {
                    vertices[index] = new Vector3(-size * 0.5f + x * step, 0f, -size * 0.5f + z * step);
                    uv[index] = new Vector2(x / (float)segments, z / (float)segments);
                    index++;
                }
            }

            index = 0;
            for (var z = 0; z < segments; z++)
            {
                for (var x = 0; x < segments; x++)
                {
                    var start = z * (segments + 1) + x;
                    triangles[index++] = start;
                    triangles[index++] = start + segments + 1;
                    triangles[index++] = start + 1;
                    triangles[index++] = start + 1;
                    triangles[index++] = start + segments + 1;
                    triangles[index++] = start + segments + 2;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void CreateCamera(Transform parent, PlayerShip player)
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraRig));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(parent, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 58f;
            camera.nearClipPlane = 0.2f;
            camera.farClipPlane = 620f;
            camera.allowHDR = true;
            camera.backgroundColor = new Color(0.31f, 0.62f, 0.78f);
            cameraObject.GetComponent<CameraRig>().Follow(player);
        }

        private static void CreateIslands(Transform parent)
        {
            CreateIsland(parent, new Vector3(-125f, -4.2f, 95f), 1.35f, 8);
            CreateIsland(parent, new Vector3(145f, -4.8f, 120f), 1.7f, 10);
            CreateIsland(parent, new Vector3(-165f, -5.2f, -90f), 1.9f, 11);
            CreateIsland(parent, new Vector3(175f, -4.6f, -80f), 1.45f, 8);
        }

        private static void CreateIsland(Transform parent, Vector3 position, float scale, int rocks)
        {
            var island = new GameObject("Rocky Island");
            island.transform.SetParent(parent, false);
            island.transform.position = position;
            island.transform.localScale = Vector3.one * scale;
            island.AddComponent<Obstacle>();

            for (var i = 0; i < rocks; i++)
            {
                var angle = i / (float)rocks * Mathf.PI * 2f;
                var radius = UnityEngine.Random.Range(3f, 10f);
                var rock = GameObject.CreatePrimitive(UnityEngine.Random.value > 0.45f ? PrimitiveType.Sphere : PrimitiveType.Capsule);
                rock.name = "Weathered Rock";
                rock.transform.SetParent(island.transform, false);
                rock.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, UnityEngine.Random.Range(1.6f, 4.2f), Mathf.Sin(angle) * radius);
                rock.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-10f, 12f), UnityEngine.Random.Range(0f, 180f), UnityEngine.Random.Range(-8f, 8f));
                rock.transform.localScale = new Vector3(UnityEngine.Random.Range(4f, 8f), UnityEngine.Random.Range(5f, 13f), UnityEngine.Random.Range(4f, 8f));
                rock.GetComponent<Renderer>().sharedMaterial = WorldMaterials.Rock;
            }

            var sand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sand.name = "Sand Shelf";
            sand.transform.SetParent(island.transform, false);
            sand.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            sand.transform.localScale = new Vector3(12f, 0.55f, 11f);
            sand.GetComponent<Renderer>().sharedMaterial = WorldMaterials.Sand;

            for (var i = 0; i < 5; i++)
            {
                CreatePalm(island.transform, new Vector3(UnityEngine.Random.Range(-6f, 6f), 4.5f, UnityEngine.Random.Range(-5f, 5f)));
            }
        }

        private static void CreatePalm(Transform parent, Vector3 localPosition)
        {
            var palm = new GameObject("Palm");
            palm.transform.SetParent(parent, false);
            palm.transform.localPosition = localPosition;
            palm.transform.localRotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), UnityEngine.Random.Range(-5f, 5f));

            var trunk = Primitive(palm.transform, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, 2.3f, 0f), new Vector3(0.35f, 2.4f, 0.35f), WorldMaterials.Trunk);
            DisableCollider(trunk);
            for (var i = 0; i < 7; i++)
            {
                var leaf = Primitive(palm.transform, PrimitiveType.Cube, "Palm Leaf", new Vector3(0f, 4.8f, 0f), new Vector3(0.22f, 0.08f, 2.7f), WorldMaterials.Leaf);
                leaf.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-18f, 6f), i * 51f, 0f);
                leaf.transform.localPosition += leaf.transform.forward * 1.7f;
                DisableCollider(leaf);
            }
        }

        internal static GameObject Primitive(Transform parent, PrimitiveType type, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
        }

        internal static void DisableCollider(GameObject gameObject)
        {
            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    public sealed class Obstacle : MonoBehaviour
    {
    }

    public static class ShipFactory
    {
        public static T Create<T>(string name, Vector3 position, Quaternion rotation, ShipTeam team) where T : ShipBase
        {
            var root = new GameObject(name, typeof(Rigidbody), typeof(BoxCollider));
            root.transform.SetPositionAndRotation(position, rotation);
            var collider = root.GetComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.15f, 0f);
            collider.size = new Vector3(5.3f, 2.25f, 11.2f);

            CreateVisuals(root.transform, team);
            root.AddComponent<WakeEmitter>();
            var ship = root.AddComponent<T>();
            ship.Initialize(team, team == ShipTeam.Player);
            return ship;
        }

        private static void CreateVisuals(Transform root, ShipTeam team)
        {
            var player = team == ShipTeam.Player;
            var hullMaterial = player ? WorldMaterials.PlayerHull : WorldMaterials.PirateHull;
            var sailMaterial = player ? WorldMaterials.PlayerSail : WorldMaterials.PirateSail;

            if (CreateImportedGalleon(root, hullMaterial, sailMaterial))
            {
                return;
            }

            var hull = new GameObject("Sculpted Hull", typeof(MeshFilter), typeof(MeshRenderer));
            hull.transform.SetParent(root, false);
            hull.GetComponent<MeshFilter>().sharedMesh = CreateHullMesh();
            hull.GetComponent<MeshRenderer>().sharedMaterial = hullMaterial;

            var deck = WorldBuilder.Primitive(root, PrimitiveType.Cube, "Deck", new Vector3(0f, 1.05f, -0.2f), new Vector3(4.8f, 0.28f, 9.4f), WorldMaterials.Deck);
            WorldBuilder.DisableCollider(deck);

            var cabin = WorldBuilder.Primitive(root, PrimitiveType.Cube, "Captain Cabin", new Vector3(0f, 2.05f, -3.75f), new Vector3(4.1f, 1.9f, 2.3f), hullMaterial);
            WorldBuilder.DisableCollider(cabin);
            var cabinRoof = WorldBuilder.Primitive(root, PrimitiveType.Cube, "Cabin Roof", new Vector3(0f, 3.08f, -3.75f), new Vector3(4.55f, 0.22f, 2.65f), WorldMaterials.Gold);
            WorldBuilder.DisableCollider(cabinRoof);

            for (var x = -1; x <= 1; x++)
            {
                var window = WorldBuilder.Primitive(root, PrimitiveType.Cube, "Cabin Window", new Vector3(x * 1.05f, 2.14f, -4.93f), new Vector3(0.58f, 0.68f, 0.10f), WorldMaterials.Window);
                WorldBuilder.DisableCollider(window);
            }

            CreateMast(root, new Vector3(0f, 1.2f, 0.9f), 9.2f, sailMaterial, true);
            CreateMast(root, new Vector3(0f, 1.2f, -2.35f), 7.1f, sailMaterial, false);
            CreateCannons(root);

            var bowRail = WorldBuilder.Primitive(root, PrimitiveType.Cube, "Bow Rail", new Vector3(0f, 2.05f, 4.5f), new Vector3(4.0f, 0.16f, 0.16f), WorldMaterials.Gold);
            bowRail.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            WorldBuilder.DisableCollider(bowRail);

            var flag = WorldBuilder.Primitive(root, PrimitiveType.Cube, "Pennant", new Vector3(0.9f, 9.3f, -2.35f), new Vector3(1.8f, 0.62f, 0.08f), sailMaterial);
            WorldBuilder.DisableCollider(flag);
        }

        private static bool CreateImportedGalleon(Transform root, Material hullMaterial, Material sailMaterial)
        {
            var prefab = Resources.Load<GameObject>("Models/StylizedGalleon");
            if (prefab == null)
            {
                return false;
            }

            var visual = UnityEngine.Object.Instantiate(prefab, root, false);
            visual.name = "Detailed Galleon";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
            {
                var part = renderer.name.ToLowerInvariant();
                if (part.Contains("sail") || part.Contains("flag"))
                {
                    renderer.sharedMaterial = sailMaterial;
                }
                else if (part.Contains("gold") || part.Contains("brass") || part.Contains("rail"))
                {
                    renderer.sharedMaterial = WorldMaterials.Gold;
                }
                else if (part.Contains("deck"))
                {
                    renderer.sharedMaterial = WorldMaterials.Deck;
                }
                else if (part.Contains("mast") || part.Contains("yard") || part.Contains("bowsprit") || part.Contains("rigging"))
                {
                    renderer.sharedMaterial = WorldMaterials.Mast;
                }
                else if (part.Contains("cannon"))
                {
                    renderer.sharedMaterial = WorldMaterials.Cannonball;
                }
                else if (part.Contains("window") || part.Contains("lantern"))
                {
                    renderer.sharedMaterial = WorldMaterials.Window;
                }
                else
                {
                    renderer.sharedMaterial = hullMaterial;
                }
            }
            return true;
        }

        private static void CreateMast(Transform root, Vector3 basePosition, float height, Material sailMaterial, bool main)
        {
            var mast = WorldBuilder.Primitive(root, PrimitiveType.Cylinder, "Mast", basePosition + Vector3.up * height * 0.5f, new Vector3(0.22f, height * 0.5f, 0.22f), WorldMaterials.Mast);
            WorldBuilder.DisableCollider(mast);

            var yardCount = main ? 3 : 2;
            for (var i = 0; i < yardCount; i++)
            {
                var y = basePosition.y + height * (0.43f + i * 0.19f);
                var width = (main ? 5.5f : 4.5f) - i * 0.65f;
                var yard = WorldBuilder.Primitive(root, PrimitiveType.Cylinder, "Yard", new Vector3(basePosition.x, y, basePosition.z), new Vector3(0.12f, width * 0.5f, 0.12f), WorldMaterials.Mast);
                yard.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                WorldBuilder.DisableCollider(yard);

                var sail = WorldBuilder.Primitive(root, PrimitiveType.Cube, "Sail", new Vector3(basePosition.x, y - 0.72f, basePosition.z), new Vector3(width * 0.86f, 1.35f, 0.10f), sailMaterial);
                sail.transform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? 2f : -2f);
                WorldBuilder.DisableCollider(sail);
            }
        }

        private static void CreateCannons(Transform root)
        {
            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < 4; i++)
                {
                    var cannon = WorldBuilder.Primitive(root, PrimitiveType.Cylinder, "Cannon", new Vector3(side * 2.52f, 1.55f, -2.7f + i * 1.8f), new Vector3(0.19f, 0.64f, 0.19f), WorldMaterials.Cannonball);
                    cannon.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    WorldBuilder.DisableCollider(cannon);
                }
            }
        }

        private static Mesh CreateHullMesh()
        {
            const int sections = 4;
            var z = new[] { -5.2f, -1.7f, 2.7f, 5.8f };
            var width = new[] { 2.25f, 2.75f, 2.35f, 0.20f };
            var bottomWidth = new[] { 1.35f, 1.65f, 1.20f, 0.08f };
            var vertices = new Vector3[sections * 4];
            for (var i = 0; i < sections; i++)
            {
                vertices[i * 4] = new Vector3(-width[i], 1f, z[i]);
                vertices[i * 4 + 1] = new Vector3(width[i], 1f, z[i]);
                vertices[i * 4 + 2] = new Vector3(-bottomWidth[i], -1.25f, z[i]);
                vertices[i * 4 + 3] = new Vector3(bottomWidth[i], -1.25f, z[i]);
            }

            var triangles = new List<int>();
            for (var i = 0; i < sections - 1; i++)
            {
                var a = i * 4;
                var b = (i + 1) * 4;
                AddQuad(triangles, a, b, b + 2, a + 2);
                AddQuad(triangles, a + 1, a + 3, b + 3, b + 1);
                AddQuad(triangles, a, a + 1, b + 1, b);
                AddQuad(triangles, a + 2, b + 2, b + 3, a + 3);
            }
            AddQuad(triangles, 0, 2, 3, 1);
            var last = (sections - 1) * 4;
            AddQuad(triangles, last, last + 1, last + 3, last + 2);

            var mesh = new Mesh { name = "Tapered Ship Hull" };
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }
    }

    public static class WorldMaterials
    {
        private static Material ocean;
        private static Material playerHull;
        private static Material pirateHull;
        private static Material playerSail;
        private static Material pirateSail;
        private static Material deck;
        private static Material gold;
        private static Material window;
        private static Material mast;
        private static Material cannonball;
        private static Material rock;
        private static Material sand;
        private static Material trunk;
        private static Material leaf;

        public static Material Ocean => ocean ??= MakeOcean();
        public static Material PlayerHull => playerHull ??= Make(new Color(0.035f, 0.12f, 0.22f), 0.25f, 0.48f);
        public static Material PirateHull => pirateHull ??= Make(new Color(0.23f, 0.035f, 0.028f), 0.18f, 0.35f);
        public static Material PlayerSail => playerSail ??= Make(new Color(0.055f, 0.20f, 0.42f), 0.05f, 0.26f);
        public static Material PirateSail => pirateSail ??= Make(new Color(0.58f, 0.055f, 0.035f), 0.05f, 0.24f);
        public static Material Deck => deck ??= Make(new Color(0.34f, 0.17f, 0.065f), 0.05f, 0.30f);
        public static Material Gold => gold ??= Make(new Color(0.84f, 0.49f, 0.11f), 0.65f, 0.72f);
        public static Material Window => window ??= Make(new Color(1f, 0.55f, 0.12f), 0.15f, 0.88f, new Color(1f, 0.22f, 0.03f) * 0.45f);
        public static Material Mast => mast ??= Make(new Color(0.18f, 0.075f, 0.025f), 0.02f, 0.24f);
        public static Material Cannonball => cannonball ??= Make(new Color(0.035f, 0.04f, 0.045f), 0.72f, 0.68f);
        public static Material Rock => rock ??= Make(new Color(0.24f, 0.30f, 0.27f), 0.02f, 0.20f);
        public static Material Sand => sand ??= Make(new Color(0.78f, 0.62f, 0.31f), 0f, 0.18f);
        public static Material Trunk => trunk ??= Make(new Color(0.30f, 0.14f, 0.045f), 0f, 0.22f);
        public static Material Leaf => leaf ??= Make(new Color(0.08f, 0.36f, 0.17f), 0f, 0.18f);

        private static Material MakeOcean()
        {
            var shader = Resources.Load<Shader>("Shaders/Ocean");
            var material = new Material(shader) { name = "Turquoise Ocean" };
            material.SetColor("_ShallowColor", new Color(0.015f, 0.48f, 0.58f, 1f));
            material.SetColor("_DeepColor", new Color(0.005f, 0.075f, 0.20f, 1f));
            material.SetColor("_FoamColor", new Color(0.78f, 0.96f, 1f, 1f));
            material.SetFloat("_WaveHeight", 0.48f);
            return material;
        }

        private static Material Make(Color color, float metallic, float smoothness, Color emission = default)
        {
            var shader = Resources.Load<Shader>("Shaders/StylizedLit");
            var material = new Material(shader) { color = color };
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            return material;
        }
    }

    public sealed class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        private Transform target;
        private Vector3 velocity;
        private float shake;

        private void Awake()
        {
            Instance = this;
        }

        public void Follow(PlayerShip player)
        {
            target = player.transform;
            transform.position = target.position + new Vector3(0f, 17f, -25f);
        }

        public void Shake(float amount)
        {
            shake = Mathf.Max(shake, amount);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desired = target.position - target.forward * 24f + Vector3.up * 15f;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.24f);
            var lookTarget = target.position + target.forward * 7f + Vector3.up * 2.1f;
            var rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 5f);

            if (shake > 0f)
            {
                transform.position += UnityEngine.Random.insideUnitSphere * shake;
                shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 1.8f);
            }
        }
    }

    public sealed class WakeEmitter : MonoBehaviour
    {
        private ParticleSystem particles;

        private void Start()
        {
            particles = Effects.CreateParticleSystem("Wake", transform);
            particles.transform.localPosition = new Vector3(0f, -0.35f, -5.1f);
            particles.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var main = particles.main;
            main.startLifetime = 2.2f;
            main.startSpeed = 2.4f;
            main.startSize = 1.2f;
            main.startColor = new Color(0.84f, 0.97f, 1f, 0.72f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 16f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.3f;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.65f), new Color(1f, 1f, 1f, 0f));
        }
    }

    public static class Effects
    {
        private static AudioClip cannonClip;
        private static AudioClip impactClip;

        public static ParticleSystem CreateParticleSystem(string name, Transform parent = null)
        {
            var gameObject = new GameObject(name, typeof(ParticleSystem));
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }
            var particles = gameObject.GetComponent<ParticleSystem>();
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Resources.Load<Shader>("Shaders/Particle"));
            return particles;
        }

        public static void MuzzleFlash(Vector3 position, Vector3 direction)
        {
            var particles = Burst("Cannon Smoke", position, 18, new Color(0.92f, 0.90f, 0.84f), 0.65f, 0.62f, 3.0f);
            particles.transform.rotation = Quaternion.LookRotation(direction);
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 24f;
        }

        public static void HitBurst(Vector3 position)
        {
            Burst("Impact", position, 20, new Color(1f, 0.46f, 0.10f), 0.45f, 0.25f, 4.5f);
            PlayClip(impactClip ??= CreateImpactClip(), position, 0.52f);
        }

        public static void WaterSplash(Vector3 position)
        {
            position.y = -0.3f;
            Burst("Water Splash", position, 24, new Color(0.78f, 0.95f, 1f), 0.75f, 0.45f, 5.2f);
        }

        public static void SinkingBurst(Vector3 position)
        {
            Burst("Sinking Blast", position + Vector3.up, 52, new Color(0.22f, 0.22f, 0.20f), 1.8f, 0.8f, 6f);
            PlayClip(cannonClip ??= CreateCannonClip(), position, 0.9f);
        }

        public static void CannonSound(Vector3 position)
        {
            PlayClip(cannonClip ??= CreateCannonClip(), position, 0.72f);
        }

        private static ParticleSystem Burst(string name, Vector3 position, int count, Color color, float lifetime, float size, float speed)
        {
            var particles = CreateParticleSystem(name);
            particles.transform.position = position;
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSize = size;
            main.startSpeed = speed;
            main.startColor = color;
            main.gravityModifier = 0.25f;
            var emission = particles.emission;
            emission.enabled = false;
            particles.Emit(count);
            UnityEngine.Object.Destroy(particles.gameObject, lifetime + 0.5f);
            return particles;
        }

        private static void PlayClip(AudioClip clip, Vector3 position, float volume)
        {
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        private static AudioClip CreateCannonClip()
        {
            return CreateNoiseClip("Cannon", 0.72f, (t, random) =>
            {
                var boom = Mathf.Exp(-t * 8f) * Mathf.Sin(t * 110f);
                var noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 5f);
                return Mathf.Clamp((boom * 0.75f + noise * 0.55f) * 1.2f, -1f, 1f);
            });
        }

        private static AudioClip CreateImpactClip()
        {
            return CreateNoiseClip("Impact", 0.28f, (t, random) =>
            {
                var noise = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 18f);
                return noise * 0.85f;
            });
        }

        private static AudioClip CreateNoiseClip(string name, float duration, Func<float, System.Random, float> sample)
        {
            const int rate = 22050;
            var count = Mathf.CeilToInt(duration * rate);
            var data = new float[count];
            var random = new System.Random(name.GetHashCode());
            for (var i = 0; i < count; i++)
            {
                data[i] = sample(i / (float)rate, random);
            }

            var clip = AudioClip.Create(name, count, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
