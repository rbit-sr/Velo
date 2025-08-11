using CEngine.Util.Misc;
using CEngine.World.Actor;
using Microsoft.Xna.Framework;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Velo
{
    public struct Filename
    {
        public string Name;
    }

    public struct Argument
    {
        public string Name;
        public Type Type;
        public bool Optional;

        public Argument(string name, Type type, bool optional = false)
        {
            Name = name;
            Type = type;
            Optional = optional;
        }
    }

    public class CommandException : Exception
    {
        public CommandException(string message) : base(message) { }
    }

    public class ParseHelper
    {
        public static string SplitHead(ref string expression)
        {
            expression = expression.TrimStart(new[] { ' ' });

            int cut = 0;
            bool quoted = false;
            if (expression.StartsWith("\""))
            {
                quoted = true;
                while (true)
                {
                    cut = expression.IndexOf("\"", cut + 1);
                    if (cut == -1 || expression[cut - 1] != '\\')
                    {
                        cut++;
                        break;
                    }
                }
            }
            else
                cut = expression.IndexOf(' ');

            string head;
            if (cut == -1)
            {
                head = expression;
                expression = "";
            }
            else
            {
                head = expression.Substring(0, cut);
                expression = expression.Substring(Math.Min(cut + 1, expression.Length));
            }
            if (quoted)
            {
                if (head.StartsWith("\""))
                    head = head.Substring(1);
                if (head.EndsWith("\""))
                    head = head.Substring(0, head.Length - 1);
            }
            head = head.Replace("\\\"", "\"");
            head = head.Replace("\\n", "\n");
            head = head.Replace("\\\\", "\\");
            return head;
        }

        public static IEnumerable<object> Match(string expression, IEnumerable<Argument> args)
        {
            int minArguments = args.Where(a => !a.Optional).Count();
            int maxArguments = args.Count();

            List<object> result = new List<object>();
            int i = 0;
            foreach (Argument argument in args)
            {
                string arg = SplitHead(ref expression);
                if (arg == "")
                {
                    if (!argument.Optional)
                        throw new CommandException($"Too few arguments! Expected: {minArguments}{(maxArguments != minArguments ? "-" + maxArguments : "")}, given: {i}");
                    break;
                }
                MethodInfo method = typeof(ParseHelper).GetMethod($"Parse_{TypeToSimpleString(argument.Type)}", BindingFlags.Public | BindingFlags.Static);
                try
                {
                    result.Add(method.Invoke(null, new object[] { arg }));
                }
                catch (TargetInvocationException e)
                {
                    throw new CommandException($"Invalid argument at position {i + 1}: {e.InnerException.Message}");
                }

                i++;
            }

            while (SplitHead(ref expression) != "")
                i++;

            if (i > maxArguments)
                throw new CommandException($"Too many arguments! Expected: {minArguments}{(maxArguments != minArguments ? "-" + maxArguments : "")}, given: {i}");

            return result;
        }

        public static string TypeToSimpleString(Type type)
        {
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(byte))
                return "byte";
            if (type == typeof(int))
                return "int";
            if (type == typeof(uint))
                return "uint";
            if (type == typeof(float))
                return "float";
            if (type == typeof(string))
                return "string";
            if (type == typeof(VeloProxy))
                return "Velo";
            return type.Name;
        }

#pragma warning disable IDE1006
        public static bool Parse_bool(string str)
        {
            string lower = str.ToLower();
            if (lower != "true" && lower != "false" && lower != "t" && lower != "f")
                throw new CommandException($"\"{str}\" is not a valid boolean (\"true or false\")!");
            return lower.StartsWith("t");
        }

        public static byte Parse_byte(string str)
        {
            if (!byte.TryParse(str, out byte result))
                throw new CommandException($"\"{str}\" is not a valid byte!");
            return result;
        }

        public static int Parse_int(string str)
        {
            if (!int.TryParse(str, out int result))
                throw new CommandException($"\"{str}\" is not a valid integer!");
            return result;
        }

        public static uint Parse_uint(string str)
        {
            if (!uint.TryParse(str, out uint result))
                throw new CommandException($"\"{str}\" is not a valid non-negative integer!");
            return result;
        }

        public static float Parse_float(string str)
        {
            if (!float.TryParse(str, out float result))
                throw new CommandException($"\"{str}\" is not a valid decimal!");
            return result;
        }

        public static string Parse_string(string str)
        {
            return str;
        }

        public static Vector2 Parse_Vector2(string str)
        {
            string[] split = str.Split(',');
            if (split.Length != 2)
                throw new CommandException($"\"{str}\" is not a valid vector, it needs to contain a single comma!");
            return new Vector2(
                Parse_float(split[0].Trim(new[] { ' ' })), 
                Parse_float(split[1].Trim(new[] { ' ' }))
            );
        }

        public static TimeSpan Parse_TimeSpan(string str)
        {
            if (!long.TryParse(str, out long result))
                throw new CommandException($"\"{str}\" is not a valid integer!");
            return new TimeSpan(result);
        }

        public static Color Parse_Color(string str)
        {
            string[] split = str.Split(',');
            if (split.Length != 3 && split.Length != 4)
                throw new CommandException($"\"{str}\" is not a valid color, it needs to contain 2 or 3 commas!");
            return new Color(
                Parse_int(split[0].Trim(new[] { ' ' })),
                Parse_int(split[1].Trim(new[] { ' ' })),
                Parse_int(split[2].Trim(new[] { ' ' })),
                split.Length == 4 ? Parse_int(split[3].Trim(new[] { ' ' })) : 255
            );
        }

        public static CEncryptedFloat Parse_CEncryptedFloat(string str)
        {
            return new CEncryptedFloat(Parse_float(str));
        }

        public static Filename Parse_Filename(string str)
        {
            if (str.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                throw new CommandException($"\"{str}\" is not a valid filename!");
            return new Filename() { Name = str };
        }

        public static string ToString_bool(bool value, int precision = 6)
        {
            return value ? "true" : "false";
        }

        public static string ToString_byte(byte value, int precision = 6)
        {
            return value.ToString();
        }

        public static string ToString_int(int value, int precision = 6)
        {
            return value.ToString();
        }

        public static string ToString_uint(uint value, int precision = 6)
        {
            return value.ToString();
        }

        public static string ToString_float(float value, int precision = 6)
        {
            return value.ToString($"F{precision}", CultureInfo.InvariantCulture);
        }

        public static string ToString_string(string value, int precision = 6)
        {
            return $"\"{value}\"";
        }

        public static string ToString_Vector2(Vector2 value, int precision = 6)
        {
            return $"{ToString_float(value.X)}, {ToString_float(value.Y)}";
        }

        public static string ToString_TimeSpan(TimeSpan value, int precision = 6)
        {
            return value.Ticks.ToString();
        }

        public static string ToString_Color(Color value, int precision = 6)
        {
            return $"{value.R}, {value.G}, {value.B}, {value.A}";
        }

        public static string ToString_CEncryptedFloat(CEncryptedFloat value, int precision = 6)
        {
            return ToString_float(value.Value, precision);
        }

        public static string ToString_Filename(Filename value, int precision = 6)
        {
            return $"\"{value.Name}\"";
        }
#pragma warning restore IDE1006
    }

    public enum EGroup : int
    {
        GAME,
        RECORDING_AND_REPLAY,
        SAVESTATE,
        TAS,
        MISCELLANEOUS
    }

    public static class EGroupExtensions
    {
        public static string Label(this EGroup group)
        {
            switch (group)
            {
                case EGroup.GAME:
                    return "Game";
                case EGroup.RECORDING_AND_REPLAY:
                    return "Recording and Replay";
                case EGroup.SAVESTATE:
                    return "Savestate";
                case EGroup.TAS:
                    return "TAS";
                case EGroup.MISCELLANEOUS:
                    return "Miscellaneous";
            }
            return "";
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string Name;
        public EGroup Group;
        public string Description;
        public string LongDescription;

        public CommandAttribute(string name, EGroup group, string description, string longDescription = null)
        {
            Name = name;
            Group = group;
            Description = description;
            LongDescription = longDescription;
            if (longDescription == null)
                LongDescription = description;
        }
    }

    public class Commands
    {
        struct Command
        {
            public MethodInfo Method;
            public CommandAttribute Attribute;
            public Argument[] Arguments;
        }

        private readonly static Command[] commands =
            typeof(Commands).GetMethods(BindingFlags.Static | BindingFlags.Public).
            Where(m => m.GetCustomAttribute<CommandAttribute>() != null).
            Select(m => 
                new Command 
                {
                    Method = m, 
                    Attribute = m.GetCustomAttribute<CommandAttribute>(),
                    Arguments = m.GetParameters().Select(p => new Argument(p.Name, p.ParameterType, p.HasDefaultValue)).ToArray()
                }).
            ToArray();

        public static string Execute(string command)
        {
            command = command.TrimEnd(new[] { ' ' });
            if (command == "")
                return "";
            string name = ParseHelper.SplitHead(ref command);

            IEnumerable<Command> candidates =
                commands.Where(c => c.Attribute.Name.ToLower() == name.ToLower());

            if (!candidates.Any())
                throw new CommandException($"Command \"{name}\" does not exist!");

            CommandException firstError = null;
            IEnumerable<object> args = null;
            Command match = new Command();
            bool success = false;
            try
            {
                match = candidates.First();
                args = ParseHelper.Match(command, match.Arguments);
                success = true;
            }
            catch (CommandException e)
            {
                firstError = e;
            }

            foreach (Command candidate in candidates.Skip(1))
            {
                if (success)
                    break;
                try
                {
                    match = candidate;
                    args = ParseHelper.Match(command, match.Arguments);
                    success = true;
                }
                catch { }
            }

            if (!success)
                throw firstError;

            int givenCount = args.Count();
            int expectedCount = match.Method.GetParameters().Length;
            if (givenCount < expectedCount)
                args = args.Concat(Enumerable.Repeat<object>(null, expectedCount - givenCount));
            args = args.Zip(match.Method.GetParameters(), (o, p) => o ?? p.DefaultValue).ToArray();

            try
            {
                return (string)match.Method.Invoke(null, args.ToArray());
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        public static void Wrap(Func<string> commandCall)
        {
            try
            {
                ConsoleM.Instance.AppendLine(commandCall());
            }
            catch (CommandException e)
            {
                ConsoleM.Instance.AppendLine(e.Message);
            }
        }

        [Command("get", EGroup.GAME, "Get the specified field's current value.",
            "Get the specified field's current value. " +
            "To specify the targeted entity, start off with the entity's type, followed optionally by \"#\" and the entity's index (zero-based). " +
            "When omitting the index, the first entity of specified type will be targeted. " +
            "For a list of targetable entities, type \"listTargets\". " +
            "The target is followed by a chain of field accessors. " +
            "For each field access, type \".\" and the field's name. " +
            "For a list of fields for each type, type \"listFields [type]\". " +
            "\"precision\" determines the number of places after the decimal point.\n" +
            "\"padding\" determines the padding; if the resulting string is shorter than \"padding\", it will be padded with extra space characters to its left until its length matches \"padding\".\n" + 
            "Examples:\n" +
            "get Player.boost\n" +
            "get Player.actor.position.x 4\n" +
            "get Obstacle#7.broken\n"
        )]
        public static string Get(string field, int precision = 6, int padding = 0)
        {
            return Fields.Get(field, precision);
        }

        [Command("set", EGroup.GAME, "Set the specified field's value.",
            "Set the specified field's value. " +
            "To specify the targeted entity, start off with the entity's type, followed optionally by \"#\" and the entity's index (zero-based). " +
            "When omitting the index, the first entity of specified type will be targeted. " +
            "For a list of targetable entities, type \"listTargets\". " +
            "The target is followed by a chain of field accessors. " +
            "For each field access, type \".\" and the field's name. " +
            "For a list of fields for each type, type \"listFields [type]\". " +
            "Fields prefixed by \"_\" cannot be set.\n" +
            "Examples:\n" +
            "set Player.boost 0.7\n" +
            "set Player.actor.velocity 1200,400\n" +
            "set Rocket#6.sprite.isVisible false\n"
        )]
        public static string Set(string field, string value)
        {
            Fields.Set(field, value);
            return "";
        }

        private static string LowercaseProperty(string property)
        {
            if (property == "X")
                return "x";
            if (property == "Y")
                return "y";
            return property;
        }

        [Command("listFields", EGroup.GAME, "List all available fields of a type.",
            "List all available fields of a type. " +
            "Fields prefixed by \"_\" are not actually stored in the game's memory and instead calculated by Velo. " +
            "They cannot be set."
        )]
        public static string ListFields(string type)
        {
            IEnumerable<Type> matches = Fields.SupportedTypes.Where(t => ParseHelper.TypeToSimpleString(t) == type);
            if (matches.Count() == 0)
                throw new CommandException($"Type \"{type}\" is not supported!");
            List<string> props =
                matches.First().GetFields(BindingFlags.Instance | BindingFlags.Public).
                Where(f => !f.Name.EndsWith("_") && Fields.SupportedTypes.Contains(f.FieldType)).
                Select(f => $"{LowercaseProperty(f.Name)} : {ParseHelper.TypeToSimpleString(f.FieldType)}").
                ToList();
            props.Sort();
            IEnumerable<string> specialProps =
               Fields.SpecialFields.
               Where(p => p.HolderType.Name == type).
               Select(p => $"{p.Name} : {ParseHelper.TypeToSimpleString(p.Type)}");
            props.AddRange(specialProps);
            return string.Join("\n", props);
        }

        [Command("listTargets", EGroup.GAME, "List all targetable types.")]
        public static string ListTypes()
        {
            List<string> types =
                Fields.SupportedTypes.Select(ParseHelper.TypeToSimpleString).ToList();
            types.Sort();
            return string.Join("\n", types);
        }

        [Command("locate", EGroup.GAME, "Locate all entities of specified type.",
            "Locate all entites of specified type. " +
            "They will be ordered by distance to Player 1."
        )]
        public static string Locate(string type)
        {
            if (!Velo.Ingame)
                throw new CommandException("You must be ingame!");
            if (!Fields.SupportedTargetTypes.Select(ParseHelper.TypeToSimpleString).Contains(type))
                throw new CommandException($"Cannot use type \"{type}\"!");
            
            List<CActor> actors = Velo.CEngineInst.World.CollisionEngine.actors;

            IEnumerable<CActor> matches = actors.Where(a => a.controller.GetType().Name == type);
            var matchesWithIndex = Enumerable.Range(0, matches.Count()).Zip(matches, (i, a) => new KeyValuePair<int, CActor>(i, a)).ToList();
            matchesWithIndex.Sort(
                (a1, a2) => 
                    (a1.Value.Position - Velo.MainPlayer.actor.Position).LengthSquared().CompareTo(
                    (a2.Value.Position - Velo.MainPlayer.actor.Position).LengthSquared())
            );
            return string.Join(
                "\n", 
                matchesWithIndex.Select(a => $"{a.Value.controller.GetType().Name}#{a.Key}: {a.Value.Position.X}, {a.Value.Position.Y}")
            );
        }

        [Command("listItems", EGroup.GAME, "List all item IDs.")]
        public static string ListItems()
        {
            return @"
0: none
1: golden hook
2: box
3: drill
4: rocket
5: bomb
6: bomb trigger
7: triple jump (unused)
8: 2 boxes
9: 3 boxes
10: sunglasses (unused)
11: 1 rocket
12: 2 rockets
13: 3 rockets
14: shockwave
15: fireball
16: freeze
17: smiley";
        }

        private static Recording GetRecording(ref Filename name)
        {
            if (name.Name == "" || name.Name == null)
                name.Name = "run";

            Recording recording = Recording.Load(name.Name);
            if (recording == null)
                throw new CommandException($"Recording \"{name.Name}\" does not exist!");
            return recording;
        }

        private static Timeline GetTimeline(ref Filename timeline)
        {
            if (timeline.Name == "" || timeline.Name == null)
                timeline.Name = "main";

            TASProject project = RecordingAndReplay.Instance.GetTASProject();
            if (!project.Timelines.TryGetValue(timeline.Name, out var timeline_) || timeline_ == null)
                throw new CommandException($"Timeline {timeline.Name} does not exist!");
            return timeline_;
        }

        [Command("replay", EGroup.RECORDING_AND_REPLAY, "Replay a saved recording or timeline of current TAS-project.",
            "Replay a saved recording or timeline of current TAS-project. " +
            "All recordings are stored under \"Velo\\recordings\". " +
            "By default, the \"run\" recording or \"main\" timeline will be chosen."
        )]
        public static string Replay(Filename name = default)
        {
            if (Velo.ModuleSolo == null)
                throw new CommandException("You must be in a solo session!");

            TASProject project = RecordingAndReplay.Instance.GetTASProject();
            IReplayable recording;
            if (project == null)
                recording = GetRecording(ref name);
            else
                recording = GetTimeline(ref name);
            RecordingAndReplay.Instance.ReplayRecording(recording);
            return $"Started replay of \"{name.Name}\".";
        }

        [Command("verify", EGroup.RECORDING_AND_REPLAY, "Verify a saved recording or timeline of current TAS-project.",
            "Verify a saved recording or timeline of current TAS-project. " +
            "All recordings are stored under \"Velo\\recordings\". " +
            "By default, the \"run\" recording or \"main\" timeline will be chosen."
        )]
        public static string Verify(Filename name = default)
        {
            if (Velo.ModuleSolo == null)
                throw new CommandException("You must be in a solo session!");

            TASProject project = RecordingAndReplay.Instance.GetTASProject();
            IReplayable recording;
            if (project == null)
                recording = GetRecording(ref name);
            else
                recording = GetTimeline(ref name);
            RecordingAndReplay.Instance.VerifyRecording(recording);
            return $"Started verification of \"{name.Name}\".";
        }

        [Command("setGhost", EGroup.RECORDING_AND_REPLAY, "Set a saved recording or timeline of current TAS-project to a ghost.",
            "Set a saved recording or timeline of current TAS-project to a ghost. " +
            "All recordings are stored under \"Velo\\recordings\". " +
            "By default, the \"run\" recording or \"main\" timeline will be chosen. " +
            "You can further specify the ghost's index (zero-based). " +
            "By default, the first ghost will be set (index 0)."
        )]
        public static string SetGhost(Filename name = default, uint index = 0)
        {
            if (Velo.ModuleSolo == null)
                throw new CommandException("You must be in a solo session!");

            TASProject project = RecordingAndReplay.Instance.GetTASProject();
            IReplayable recording;
            if (project == null)
                recording = GetRecording(ref name);
            else
            {
                Timeline timeline = GetTimeline(ref name);
                if (name.Name == "main")
                    timeline = timeline.ShallowClone();
                recording = timeline;
            }
            int actualIndex =
                RecordingAndReplay.Instance.SetGhostRecording(recording, (int)index);
            return $"Set \"{name.Name}\" to ghost {actualIndex}.";
        }

        [Command("capture", EGroup.RECORDING_AND_REPLAY, "Makes a video recording of a saved recording or timeline of current TAS-project.",
            "Makes a video recording of a saved recording or timeline of current TAS-project. " +
            "All recordings are stored under \"Velo\\recordings\". " +
            "By default, the \"run\" recording or \"main\" timeline will be chosen. " +
            "You can specify further video settings under \"Offline Game Mods\" -> \"video capture\". " +
            "The video will be saved under \"Velo\\videos\". " +
            "You are free to minimize the game while capturing."
        )]
        public static string Capture(Filename name = default, Filename outputName = default)
        {
            if (Velo.ModuleSolo == null)
                throw new CommandException("You must be in a solo session!");
            if (!File.Exists("ffmpeg.exe"))
                throw new CommandException("Could not find \"ffmpeg.exe\"! Download from https://www.gyan.dev/ffmpeg/builds/ and put it in the same directory as \"SpeedRunners.exe\".");
           
            TASProject project = RecordingAndReplay.Instance.GetTASProject();
            IReplayable recording;
            if (project == null)
            {
                recording = GetRecording(ref name);
                outputName.Name = name.Name + ".mp4";
            }
            else
            {
                recording = GetTimeline(ref name);
                outputName.Name = $"{project.Name} - {name.Name}.mp4";
            }

            CaptureParams captureParams = new CaptureParams
            {
                CaptureRate = OfflineGameMods.Instance.CaptureRate.Value,
                VideoRate = OfflineGameMods.Instance.VideoRate.Value,
                Width = Velo.GraphicsDevice.Viewport.Width,
                Height = Velo.GraphicsDevice.Viewport.Height,
                Crf = OfflineGameMods.Instance.Crf.Value,
                Preset = OfflineGameMods.Instance.Preset.Value.ToString().ToLower(),
                PixelFormat = OfflineGameMods.Instance.PixelFormat.Value.ToString().ToLower(),
                Filename = outputName.Name
            };

            RecordingAndReplay.Instance.CaptureRecording(recording, captureParams);
            ConsoleM.Instance.Enabled.Disable();
            return "";
        }

        [Command("stopReplay", EGroup.RECORDING_AND_REPLAY, "Stops the current replay or verification.")]
        public static string StopReplay()
        {
            if (!RecordingAndReplay.Instance.IsPlaybackRunning)
                throw new CommandException("There is no active replay!");
            RecordingAndReplay.Instance.StopPlayback(notification: true);
            return "Stopped replay.";
        }

        [Command("saveLast", EGroup.RECORDING_AND_REPLAY, "Save the recording for the last run.",
            "Save the recording for the last run. " +
            "All recordings are stored under \"Velo\\recordings\"."
        )]
        public static string SaveLast(Filename name)
        {
            if (Velo.ModuleSolo == null)
                throw new CommandException("You must be in a solo session!");

            if (!RecordingAndReplay.Instance.SaveLastRecording(name.Name))
                throw new CommandException("There is nothing to save!");
            return $"Saved recording as \"{name.Name}\".";
        }

        [Command("download", EGroup.RECORDING_AND_REPLAY, "Downloads a recording from the leaderboard.",
            "Downloads a recording from the leaderboard. " +
            "You can see the respective ID when clicking the run on the leaderboard."
        )]
        public static string Download(uint id, Filename name)
        {
            RunsDatabase.Instance.RequestRecordingCached((int)id, recording =>
            {
                RecordingAndReplay.Instance.SaveRecording(recording, name.Name);
                Velo.AddOnPreUpdateTS(() => ConsoleM.Instance.AppendLine($"Successfully downloaded {id}."));
            },
                (error) => ConsoleM.Instance.AppendLine(error.Message)
            );
            return "Downloading...";
        }

        [Command("deleteRec", EGroup.RECORDING_AND_REPLAY, "Delete a recording.",
            "Delete a recording. " +
            "All recordings are stored under \"Velo\\recordings\"."
        )]
        public static string DeleteRec(Filename name)
        {
            if (!File.Exists($"Velo\\recordings\\{name.Name}.srrec"))
                throw new CommandException($"The recording \"{name.Name}\" does not exist!");

            File.Delete($"Velo\\recordings\\{name.Name}.srrec");
            return $"Deleted \"{name.Name}\".";
        }

        [Command("listRec", EGroup.RECORDING_AND_REPLAY, "List all recordings.",
            "List all recordings. " +
            "All recordings are stored under \"Velo\\recordings\"."
        )]
        public static string ListRec()
        {
            return string.Join(
                "\n",
                Directory.EnumerateFiles("Velo\\recordings").
                Where(n => n.ToLower().EndsWith(".srrec")).
                Select(Path.GetFileName).
                Select(n => n.Substring(0, n.Length - ".srrec".Length))
            );
        }
        
        [Command("freeze", EGroup.RECORDING_AND_REPLAY, "Freezes the game.")]
        public static string Freeze()
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            OfflineGameMods.Instance.Pause();
            return "";
        }

        [Command("unfreeze", EGroup.RECORDING_AND_REPLAY, "Unfreezes the game.")]
        public static string Unfreeze()
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            OfflineGameMods.Instance.Unpause();
            return "";
        }

        [Command("step", EGroup.RECORDING_AND_REPLAY, "Unfreeze the game for the specified amount of frames.")]
        public static string Step(uint frames = 1)
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            OfflineGameMods.Instance.StepFrames((int)frames);
            return "";
        }

        [Command("jump", EGroup.RECORDING_AND_REPLAY, "Jump frames forward.")]
        public static string Jump(uint frames)
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            RecordingAndReplay.Instance.OffsetFrames((int)frames);
            return "";
        }

        [Command("jumpBack", EGroup.RECORDING_AND_REPLAY, "Jump frames back.")]
        public static string JumpBack(uint frames)
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            RecordingAndReplay.Instance.OffsetFrames(-(int)frames);
            return "";
        }

        [Command("jumpTo", EGroup.RECORDING_AND_REPLAY, "Jump to frame.")]
        public static string JumpTo(int frame)
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            RecordingAndReplay.Instance.JumpToFrame(frame);
            return "";
        }

        [Command("jumpToEnd", EGroup.RECORDING_AND_REPLAY, "Jump to the last frame.")]
        public static string JumpToEnd()
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            RecordingAndReplay.Instance.JumpToFrame(int.MaxValue / 2); // just a big number
            return "";
        }

        [Command("jumpSec", EGroup.RECORDING_AND_REPLAY, "Jump seconds forward.")]
        public static string JumpSec(float seconds)
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            RecordingAndReplay.Instance.OffsetSeconds(seconds);
            return "";
        }

        [Command("jumpBackSec", EGroup.RECORDING_AND_REPLAY, "Jump seconds back.")]
        public static string JumpBackSec(float seconds)
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            RecordingAndReplay.Instance.OffsetSeconds(-seconds);
            return "";
        }

        [Command("jumpToSec", EGroup.RECORDING_AND_REPLAY, "Jump to second.")]
        public static string JumpToSec(float second)
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            RecordingAndReplay.Instance.JumpToSecond(second);
            return "";
        }

        [Command("stepTo", EGroup.RECORDING_AND_REPLAY, "Step to frame.")]
        public static string StepTo(uint frame)
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            RecordingAndReplay.Instance.StepToFrame((int)frame);
            return "";
        }

        [Command("saveSS", EGroup.SAVESTATE, "Create a new savestate.",
            "Create a new savestate. " +
            "All savestates are stored under \"Velo\\savestates\". " +
            "When recording a TAS, this command will instead refer to the TAS-project's timeline collection."
        )]
        public static string SaveSS(Filename name)
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            OfflineGameMods.Instance.SavestateManager.Save(name.Name);
            return $"Saved \"{name.Name}\".";
        }

        [Command("loadSS", EGroup.SAVESTATE, "Load a savestate.",
            "Load a savestate. " +
            "All savestates are stored under \"Velo\\savestates\". " +
            "When recording a TAS, this command will instead refer to the TAS-project's timeline collection."
        )]
        public static string LoadSS(Filename name)
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            if (!OfflineGameMods.Instance.SavestateManager.Load(name.Name))
                throw new CommandException($"Savestate \"{name.Name}\" does not exist!");
            return $"Loaded \"{name.Name}\".";
        }

        [Command("undoSS", EGroup.SAVESTATE, "Restore the state before loading the previous savestate.")]
        public static string UndoSS()
        {
            if (Velo.Online)
                throw new CommandException("You cannot do this in an online match!");
            if (!OfflineGameMods.Instance.SavestateManager.Undo())
                throw new CommandException("Nothing to undo!");
            return "";
        }

        [Command("renameSS", EGroup.SAVESTATE, "Rename a savestate.",
            "Rename a savestate. " +
            "All savestates are stored under \"Velo\\savestates\". " +
            "When recording a TAS, this command will instead refer to the TAS-project's timeline collection."
        )]
        public static string RenameSS(Filename oldName, Filename newName)
        {
            if (!OfflineGameMods.Instance.SavestateManager.Rename(oldName.Name, newName.Name))
                throw new CommandException($"Savestate \"{oldName.Name}\" does not exist!");
            return $"Renamed savestate \"{oldName.Name}\" to \"{newName.Name}\".";
        }

        [Command("copySS", EGroup.SAVESTATE, "Copy a savestate.",
            "Copy a savestate. " +
            "All savestates are stored under \"Velo\\savestates\". " +
            "When recording a TAS, this command will instead refer to the TAS-project's timeline collection."
        )]
        public static string CopySS(Filename sourceName, Filename targetName)
        {
            if (!OfflineGameMods.Instance.SavestateManager.Rename(sourceName.Name, targetName.Name))
                throw new CommandException($"Savestate \"{sourceName.Name}\" does not exist!");
            return $"Copied savestate \"{sourceName.Name}\" to \"{targetName.Name}\".";
        }

        [Command("deleteSS", EGroup.SAVESTATE, "Delete a savestate.",
            "Delete a savestate. " +
            "All savestates are stored under \"Velo\\savestates\". " +
            "When recording a TAS, this command will instead refer to the TAS-project's timeline collection."
        )]
        public static string DeleteSS(Filename name)
        {
            if (!OfflineGameMods.Instance.SavestateManager.Delete(name.Name))
                throw new CommandException($"Savestate \"{name.Name}\" does not exist!");
            return $"Deleted savestate \"{name.Name}\".";
        }

        [Command("listSS", EGroup.SAVESTATE, "List all savestates.",
            "List all savestates. " +
            "All savestates are stored under \"Velo\\savestates\". " +
            "When recording a TAS, this command will instead refer to the TAS-project's timeline collection."
        )]
        public static string ListSS()
        {
            return string.Join("\n", OfflineGameMods.Instance.SavestateManager.List());
        }

        [Command("clearSS", EGroup.SAVESTATE, "Delete all savestates.",
            "Delete all savestates. " +
            "All savestates are stored under \"Velo\\savestates\". " +
            "When recording a TAS, this command will instead refer to the TAS-project's timeline collection."
        )]
        public static string ClearSS()
        {
            OfflineGameMods.Instance.SavestateManager.Clear();
            return "Cleared all savestates.";
        }

        [Command("newTAS", EGroup.TAS, "Create a new TAS-project.")]
        public static string NewTAS(Filename name)
        {
            if (Velo.ModuleSolo == null)
                throw new CommandException("You must be in a solo session!");

            if (File.Exists($"Velo\\TASprojects\\{name.Name}.srtas"))
                throw new CommandException($"The TAS-project \"{name.Name}\" already exists!");

            TASProject project = RecordingAndReplay.Instance.GetTASProject();
            if (project != null)
                throw new CommandException($"The current TAS-project \"{project.Name}\" needs to be closed before creating a new one!");

            RecordingAndReplay.Instance.CreateNewTAS(name.Name);
            return $"Created new TAS-project \"{name.Name}\".";
        }

        [Command("saveTAS", EGroup.TAS, "Save the current TAS-project.",
            "Saves the current TAS-project. " +
            "All TAS-projects are stored under \"Velo\\TASprojects\". " +
            "The file is not compressed by default."
        )]
        public static string SaveTAS(bool compress = false)
        {
            TASProject project = RecordingAndReplay.Instance.GetTASProject() ?? 
                throw new CommandException("There is no TAS-project open!");
            if (RecordingAndReplay.Instance.Recorder is TASRecorder tasRecorder)
            {
                tasRecorder.Save(compress);
            }
            
            return $"Saved \"{project.Name}\".";
        }

        [Command("loadTAS", EGroup.TAS, "Load a TAS-project.",
            "Loads a TAS-project. " +
            "All TAS-projects are stored under \"Velo\\TASprojects\"."
        )]
        public static string LoadTAS(Filename name)
        {
            if (Velo.ModuleSolo == null)
                throw new CommandException("You must be in a solo session!");

            TASProject project = RecordingAndReplay.Instance.GetTASProject();
            if (project != null)
                throw new CommandException($"The current TAS-project \"{project.Name}\" needs to be closed before loading a new one!");

            project = TASProject.Load(name.Name);
            if (project == null)
                throw new CommandException($"The TAS-project \"{name.Name}\" does not exist!");

            RecordingAndReplay.Instance.SetTASProject(project);
            return $"Loaded \"{project.Name}\".\nAuthor: {SteamCache.GetPlayerName(project.Main.Info.PlayerId)}\nRerecords: {project.Rerecords}";
        }

        [Command("closeTAS", EGroup.TAS, "Close the current TAS-project.")]
        public static string CloseTAS()
        {
            TASProject project = RecordingAndReplay.Instance.GetTASProject() ??
                throw new CommandException("There is no TAS-project open!");
            TASRecorder recorder = RecordingAndReplay.Instance.Recorder as TASRecorder;
            if (recorder.NeedsSave)
            {
                recorder.NeedsSave = false;
                return "Warning: You haven't saved your TAS-project yet! Enter this command again to close without saving.";
            }

            RecordingAndReplay.Instance.CloseTASProject();
            return $"Closed \"{project.Name}\".";
        }

        [Command("deleteTAS", EGroup.TAS, "Delete a TAS-project.",
            "Delete a TAS-project. " +
            "All TAS-projects are stored under \"Velo\\TASprojects\"."
        )]
        public static string DeleteTAS(Filename name)
        {
            TASProject project = RecordingAndReplay.Instance.GetTASProject();
            if (project != null && project.Name.ToLower() == name.Name.ToLower())
                throw new CommandException($"The current TAS-project \"{project.Name}\" needs to be closed before deleting it!");

            if (!File.Exists($"Velo\\TASprojects\\{name.Name}.srtas"))
                throw new CommandException($"The TAS-project \"{name.Name}\" does not exist!");

            File.Delete($"Velo\\TASprojects\\{name.Name}.srtas");
            return $"Deleted \"{name.Name}\".";
        }

        [Command("listTAS", EGroup.TAS, "List all TAS-projects.",
            "List all TAS-projects. " +
            "All TAS-projects are stored under \"Velo\\TASprojects\"."
        )]
        public static string ListTAS()
        {
            return string.Join(
                "\n", 
                Directory.EnumerateFiles("Velo\\TASprojects").
                Where(n => n.ToLower().EndsWith(".srtas")).
                Select(Path.GetFileName).
                Select(n => n.Substring(0, n.Length - ".srtas".Length))
            );
        }

        [Command("renameTAS", EGroup.TAS, "Rename the current TAS-project.",
            "Rename the current TAS-project. " +
            "Note that this will also perform a save."
        )]
        public static string RenameTAS(Filename newName)
        {
            TASProject project = RecordingAndReplay.Instance.GetTASProject() ??
                throw new CommandException("There is no TAS-project open!");

            if (File.Exists($"Velo\\TASprojects\\{newName.Name}.srtas"))
                throw new CommandException($"The TAS-project \"{newName.Name}\" already exists!");

            string oldName = project.Name;
            project.Name = newName.Name;

            if (File.Exists($"Velo\\TASprojects\\{oldName}.srtas"))
                File.Move($"Velo\\TASprojects\\{oldName}.srtas", $"Velo\\TASprojects\\{newName.Name}.srtas");

            if (RecordingAndReplay.Instance.Recorder is TASRecorder tasRecorder)
            {
                tasRecorder.Save(false);
            }

            return $"Renamed \"{oldName}\" to \"{newName.Name}\".";
        }

        [Command("getRerecords", EGroup.TAS, "Get the rerecords count.")]
        public static string GetRerecords()
        {
            TASProject project = RecordingAndReplay.Instance.GetTASProject() ??
                throw new CommandException("There is no TAS-project open!");
            return project.Rerecords.ToString();
        }

        [Command("setRerecords", EGroup.TAS, "Set the rerecords count.")]
        public static string SetRerecords(uint count)
        {
            TASProject project = RecordingAndReplay.Instance.GetTASProject() ??
                throw new CommandException("There is no TAS-project open!");
            project.Rerecords = (int)count;
            return "";
        }

        [Command("insertFrames", EGroup.TAS, "Insert blank new frames at specified position.",
            "Insert blank new frames at specified position. " +
            "Note that these new frames and every frame afterwards will be marked as red. " +
            "You cannot insert new frames before the very first frame."
        )]
        public static string InsertFrames(int at, uint count)
        {
            TASProject project = RecordingAndReplay.Instance.GetTASProject() ??
                throw new CommandException("There is no TAS-project open!");

            int atAbsolute = at + project.Main.LapStart;

            if (atAbsolute <= 0)
                throw new CommandException($"New frames cannot be inserted at or before position {-project.Main.LapStart}!");
            if (atAbsolute > project.Main.Count)
                throw new CommandException("The specified position exceeds the timeline's length!");
            project.Main.InsertNew(atAbsolute, (int)count, new TimeSpan(OfflineGameMods.Instance.DeltaTime.Value));
            (RecordingAndReplay.Instance.Recorder as TASRecorder).SetGreenPosition(atAbsolute - 1);
            return "";
        }

        [Command("deleteFrames", EGroup.TAS, "Delete a specified range of frames.",
            "Delete a specified range of frames. " +
            "Note that every frame afterwards will be marked as red. " +
            "You cannot delete the very first frame."
        )]
        public static string DeleteFrames(int first, uint count)
        {
            TASProject project = RecordingAndReplay.Instance.GetTASProject() ??
                throw new CommandException("There is no TAS-project open!");

            int firstAbsolute = first + project.Main.LapStart;

            if (firstAbsolute <= 0)
                throw new CommandException($"Cannot delete frames at or before position {-project.Main.LapStart}!");
            if (first + count > project.Main.Count)
                throw new CommandException("The specified range exceeds the timeline's length!");
            project.Main.Delete(firstAbsolute, (int)count);
            (RecordingAndReplay.Instance.Recorder as TASRecorder).SetGreenPosition(firstAbsolute - 1);
            return "";
        }

        [Command("setLapReset", EGroup.TAS, "Enable or disable lap reset for a specified frame.",
            "Enable or disable lap reset for a specified frame. " +
            "Note that every frame afterwards will be marked as red. " +
            "You cannot modify the very first frame."
        )]
        public static string SetLapReset(int at, bool enable)
        {
            TASProject project = RecordingAndReplay.Instance.GetTASProject() ??
                throw new CommandException("There is no TAS-project open!");

            int atAbsolute = at + project.Main.LapStart;

            if (atAbsolute <= 0)
                throw new CommandException($"Cannot modify frames at or before position {-project.Main.LapStart}!");
            if (atAbsolute > project.Main.Count)
                throw new CommandException("The specified position exceeds the timeline's length!");
            Frame frame = project.Main[atAbsolute];
            frame.SetFlag(Frame.EFlag.RESET_LAP, enable);
            project.Main[atAbsolute] = frame;
            (RecordingAndReplay.Instance.Recorder as TASRecorder).SetGreenPosition(atAbsolute - 1);
            return "";
        }

        private static string CreateHelpText(IEnumerable<Command> commands, bool longDescription)
        {
            string text = "";
            EGroup prevGroup = (EGroup)(-1);
            foreach (Command command in commands)
            {
                if (command.Attribute.Group != prevGroup)
                {
                    prevGroup = command.Attribute.Group;
                    text += $"$[b:true]$[in:0]\n{command.Attribute.Group.Label()}:$[b:false]\n";
                }
                text += $"$[i:true]$[in:2]{command.Attribute.Name}";
                foreach (Argument argument in command.Arguments)
                {
                    string optionalMarker = argument.Optional ? "?" : "";
                    text += $" [{argument.Name}{optionalMarker}]";
                }
                text += $"$[i:false]\n$[in:4]{(longDescription ? command.Attribute.LongDescription : command.Attribute.Description)}\n";
            }
            return text;
        }

        [Command("help", EGroup.MISCELLANEOUS, "List all commands.",
            "List all commands. " +
            "Arguments marked with '?' are optional."
        )]
        public static string Help()
        {
            List<Command> list = commands.ToList();
            list.Sort((c1, c2) =>
            {
                if (c1.Attribute.Group != c2.Attribute.Group)
                    return c1.Attribute.Group.CompareTo(c2.Attribute.Group);
                return c1.Attribute.Name.CompareTo(c2.Attribute.Name);
            });

            return CreateHelpText(list, false);
        }

        [Command("help", EGroup.MISCELLANEOUS, "Get more detailed information about a command.",
            "Get more detailed information about a command. " +
            "Arguments marked with '?' are optional."
        )]
        public static string Help(string command = null)
        {
            IEnumerable<Command> commands = Commands.commands.Where(c => c.Attribute.Name.ToLower() == command.ToLower());
            if (!commands.Any())
                throw new CommandException($"Command \"{command}\" does not exist!");

            return CreateHelpText(commands, true);
        }

        [Command("version", EGroup.MISCELLANEOUS, "Show the current Velo version.")]
        public static string Version_()
        {
            return $"Velo {Version.VERSION_NAME} (r{Version.VERSION}) - {Version.AUTHOR}";
        }

        [Command("clear", EGroup.MISCELLANEOUS, "Clear the console.")]
        public static string Clear()
        {
            Velo.AddOnPreUpdate(() => ConsoleM.Instance.Clear());
            return "";
        }

        [Command("quit", EGroup.MISCELLANEOUS, "Close the game.")]
        public static string Quit()
        {
            Main.game.Dispose();
            SteamAPI.Shutdown();
            Environment.Exit(0);
            return "";
        }

        [Command("echo", EGroup.MISCELLANEOUS, "Print a message to the console.")]
        public static string Echo(string message)
        {
            return message;
        }

        [Command("reloadStatsMessage", EGroup.MISCELLANEOUS, "Reload the stats message.",
            "Reload the stats message. " +
            "Check out \"Console\" -> \"general\" -> \"print stats\" for more information."
        )]
        public static string ReloadStatsMessage()
        {
            if (!StatsMessage.Reload())
                throw new CommandException("\"Velo/_statsMessage.txt\" does not exist!");
            return "";
        }

        [Command("hotReload", EGroup.MISCELLANEOUS, "Perform a hot reload.",
            "Performs a hot reload. " +
            "Allows you to modify textures without having to restart the game."
        )]
        public static string HotReload(Filename filePath)
        {
            bool result = Miscellaneous.Instance.ReloadContent(filePath.Name);
            if (!result)
                throw new CommandException($"File \"{filePath.Name}\" does not exist!");
            return "";
        }
    }
}
