using CEngine.Graphics.Camera;
using CEngine.Graphics.Camera.Modifier;
using CEngine.Graphics.Component;
using CEngine.Util.Misc;
using CEngine.World.Actor;
using CEngine.World.Collision.Shape;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Velo
{
    class VeloProxy { }

    struct SpecialField
    {
        public Type HolderType;
        public Type Type;
        public string Name;
        public Func<object, object> Getter;
    }

    class Fields
    {
        public static HashSet<Type> SupportedTargetTypes = new HashSet<Type>
        {
            typeof(VeloProxy),
            typeof(Player),
            typeof(AIVolume),
            typeof(Bookcase),
            typeof(BouncePad),
            typeof(Deco),
            typeof(DecoLight),
            typeof(DecoGlow),
            typeof(DecoText),
            typeof(DroppedBomb),
            typeof(DroppedObstacle),
            typeof(Fireball),
            typeof(FreezeRay),
            typeof(GoldenHook),
            typeof(Grapple),
            typeof(Laser),
            typeof(Leaves),
            typeof(Lever),
            typeof(Obstacle),
            typeof(Pickup),
            typeof(Rocket),
            typeof(RocketLauncher),
            typeof(Rope),
            typeof(Shockwave),
            typeof(StraightRocket),
            typeof(SwitchBlock),
            typeof(Timer),
            typeof(Trigger),
            typeof(TriggerSaw),
            typeof(Tunnel),
            typeof(CCamera),
            typeof(SoloCameraModifier)
        };

        public static HashSet<string> SupportedTargetTypesStr = SupportedTargetTypes.Select(t => ParseHelper.TypeToSimpleString(t)).ToHashSet();

        public static HashSet<Type> SupportedTypes = new[]
        {
            typeof(bool),
            typeof(byte),
            typeof(int),
            typeof(float),
            typeof(string),
            typeof(Vector2),
            typeof(TimeSpan),
            typeof(Color),
            typeof(CAABB),
            typeof(Rectangle),
            typeof(Matrix),
            typeof(CEncryptedFloat),
            typeof(CActor),
            typeof(CAnimatedSpriteDrawComponent),
            typeof(CSpriteDrawComponent),
            typeof(CImageDrawComponent),
            typeof(CTextDrawComponent),
            typeof(ShakeCameraModifier),
            typeof(ClampCameraModifier),
            typeof(EditableEnum),
            typeof(EditableBool),
            typeof(EditableFloat),
            typeof(EditableInt),
            typeof(EditableString)
        }.Concat(SupportedTargetTypes).ToHashSet();

        public static HashSet<string> SupportedTypesStr = SupportedTypes.Select(t => ParseHelper.TypeToSimpleString(t)).ToHashSet();

        public static List<SpecialField> SpecialFields = new List<SpecialField>
        {
            new SpecialField{ HolderType = typeof(VeloProxy), Type = typeof(int), Name = "frame", Getter = o =>
            {
                return RecordingAndReplay.Instance.PrimarySeekable?.Frame ?? 0;
            } },
            new SpecialField{ HolderType = typeof(VeloProxy), Type = typeof(float), Name = "time", Getter = o =>
            {
                return (float)(RecordingAndReplay.Instance.PrimarySeekable?.Time.TotalSeconds ?? 0d);
            } },
            new SpecialField{ HolderType = typeof(Vector2), Type = typeof(float), Name = "_a", Getter = o =>
            {
                return ((Vector2)o).Length();
            } },
            new SpecialField{ HolderType = typeof(CActor), Type = typeof(Vector2), Name = "_acceleration", Getter = o =>
            {
                CActor actor = (CActor)o;
                if (Velo.VelocityPrev.Count <= actor.Id)
                    return Vector2.Zero;
                return (actor.Velocity - Velo.VelocityPrev[actor.Id]) / (float)Velo.GameDeltaPreFreeze.TotalSeconds;
            } },
            new SpecialField{ HolderType = typeof(Player), Type = typeof(float), Name = "_grappleCooldown", Getter = o =>
            {
                return Math.Max(Velo.get_grapple_cooldown() - (float)(Velo.GameTime - ((Player)o).grappleTime).TotalSeconds, 0f);
            } },
            new SpecialField{ HolderType = typeof(Player), Type = typeof(float), Name = "_slideCooldown", Getter = o =>
            {
                return Math.Max(Velo.get_slide_cooldown() - (float)(Velo.GameTime - ((Player)o).slideTime).TotalSeconds, 0f);
            } },
            new SpecialField{ HolderType = typeof(Player), Type = typeof(float), Name = "_surfCooldown", Getter = o =>
            {
                return Math.Max(Velo.get_jump_duration() - (float)(Velo.GameTime - ((Player)o).jumpTime).TotalSeconds, 0f);
            } }
        };

        public static Dictionary<string, SpecialField> SpecialFieldsLookup = SpecialFields.ToDictionary(p => p.Name, p => p);

        private static object GetTarget(string target)
        {
            if (target == "Player")
                return Velo.MainPlayer;
            
            int targetNumber = 0;
            string[] split = target.Split('#');
            if (split.Length == 0)
                throw new CommandException("Target cannot be empty!");
            if (!SupportedTargetTypesStr.Contains(split[0]))
                throw new CommandException($"Cannot use type \"{split[0]}\" as target!");
            if (split.Length >= 2)
            {
                if (!int.TryParse(split[1], out targetNumber))
                    throw new CommandException($"Target number \"{split[1]}\" needs to be a valid integer!");
            }
            target = split[0];

            object obj = null;

            if (target == "Velo")
            {
                if (targetNumber == 0)
                    obj = new VeloProxy();
            }
            else if (target == "CCamera")
            {
                if (targetNumber == 0 && Velo.CEngineInst.CameraManager.CameraCount >= 2)
                    obj = Velo.CEngineInst.CameraManager.GetCamera(1);
            }
            else if (target == "SoloCameraModifier")
            {
                obj = Velo.ModuleSolo?.camera1;
            }
            else
            {
                int i = targetNumber;
                List<CActor> actors = Velo.CEngineInst.World.CollisionEngine.actors;
                foreach (CActor actor in actors)
                {
                    if (actor.controller.GetType().Name == target)
                    {
                        if (i > 0)
                        {
                            i--;
                            continue;
                        }

                        obj = actor.Controller;
                        break;
                    }
                }
            }

            if (obj == null)
                throw new CommandException($"Target \"{target}#{targetNumber}\" does not exist!");
            return obj;
        }

        public static string Get(string field, int precision = 6, int padding = 0)
        {
            if (!Velo.Ingame)
                throw new CommandException("You must be ingame!");

            string[] fieldPath = field.Split('.');

            if (fieldPath.Length <= 1)
                throw new CommandException("Field-path cannot be empty!");

            object obj = GetTarget(fieldPath[0]);

            for (int i = 1; i < fieldPath.Length; i++)
            {
                if (SpecialFieldsLookup.TryGetValue(fieldPath[i], out SpecialField special))
                {
                    obj = special.Getter(obj);
                    continue;
                }
                if (fieldPath[i] == "x")
                    fieldPath[i] = "X";
                if (fieldPath[i] == "y")
                    fieldPath[i] = "Y";

                FieldInfo fieldInfo = obj.GetType().GetField(fieldPath[i], BindingFlags.Public | BindingFlags.Instance);
                if (fieldInfo == null)
                    throw new CommandException($"\"{ParseHelper.TypeToSimpleString(obj.GetType())}\" has no field \"{fieldPath[i]}\"!");
                obj = fieldInfo.GetValue(obj);
            }

            MethodInfo method = typeof(ParseHelper).GetMethod($"ToString_{ParseHelper.TypeToSimpleString(obj.GetType())}", BindingFlags.Public | BindingFlags.Static) ??
                throw new CommandException($"Type \"{ParseHelper.TypeToSimpleString(obj.GetType())}\" cannot be printed!");
            string result = (string)method.Invoke(null, new object[] { obj, precision });
            return result.PadLeft(padding);
        }

        private static object ModifyField(object obj, IEnumerable<string> fieldPath, Func<Type, object> value)
        {
            string field = fieldPath.First();
            if (field == "x")
                field = "X";
            if (field == "y")
                field = "Y";
            FieldInfo fieldInfo = obj.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance) ??
                throw new CommandException($"\"{ParseHelper.TypeToSimpleString(obj.GetType())}\" has no field \"{fieldPath.First()}\"!");
            if (fieldPath.Count() == 1)
            {
                fieldInfo.SetValue(obj, value(fieldInfo.FieldType));
                return obj;
            }
            fieldInfo.SetValue(obj, ModifyField(fieldInfo.GetValue(obj), fieldPath.Skip(1), value));
            return obj;
        }

        public static void Set(string field, string value)
        {
            if (!Velo.Ingame)
                throw new CommandException("You must be ingame!");
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");

            string[] propertyPath = field.Split('.');

            if (propertyPath.Length <= 1)
                throw new CommandException("Field-path cannot be empty!");

            object obj = GetTarget(propertyPath[0]);

            ModifyField(obj, propertyPath.Skip(1), t =>
            {
                MethodInfo method = typeof(ParseHelper).GetMethod($"Parse_{ParseHelper.TypeToSimpleString(t)}", BindingFlags.Public | BindingFlags.Static) ??
                    throw new CommandException($"Type \"{ParseHelper.TypeToSimpleString(t)}\" cannot be modified!");
                return method.Invoke(null, new object[] { value });
            });
            Velo.Poisoned = true;
        }
    }
}
