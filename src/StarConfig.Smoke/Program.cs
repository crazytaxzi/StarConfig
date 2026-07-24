using StarConfig;
using System.Xml.Linq;

var xml = """
<ActionMaps>
 <ActionProfiles version="1" optionsVersion="2" rebindVersion="2" profileName="HOSAS Test">
  <deviceoptions name=" VKBsim Gladiator EVO R    {0200231D-0000-0000-0000-504944564944}">
   <option input="x" deadzone="0.069300003"/>
  </deviceoptions>
  <options type="keyboard" instance="1" Product="Keyboard"/>
  <options type="joystick" instance="1" Product=" VKBsim Gladiator EVO R    {0200231D-0000-0000-0000-504944564944}"/>
  <options type="joystick" instance="2" Product=" VKBsim Gladiator EVO L    {0201231D-0000-0000-0000-504944564944}"/>
  <actionmap name="spaceship_movement">
   <action name="v_atc_loading_area_request"><rebind input="js2_button4" multiTap="2"/></action>
   <action name="v_autoland"><rebind input="js2_button4"/></action>
   <action name="v_strafe_longitudinal"><rebind input="js2_y"/></action>
   <action name="v_roll"><rebind input="js1_x"/></action>
   <action name="v_operator_mode_cycle"><rebind input="js1_button7"/></action>
   <action name="v_master_mode_cycle"><rebind input="js1_button8"/></action>
  </actionmap>
  <actionmap name="vehicle_movement">
   <action name="vehicle_throttle_abs"><rebind input="js2_y"/></action>
   <action name="vehicle_steer"><rebind input="js1_x"/></action>
  </actionmap>
  <actionmap name="spaceship_mining">
   <action name="v_mining_throttle"><rebind input="js2_z"/></action>
  </actionmap>
  <actionmap name="player">
   <action name="jump"><rebind input="js2_button2"/></action>
   <action name="moveforward"><rebind input="js2_ "/></action>
  </actionmap>
  <actionmap name="turret_movement">
   <action name="turret_elevation"><rebind input="js2_y"/></action>
  </actionmap>
  <actionmap name="player_input_optical_tracking">
   <action name="foip_pushtotalk"><rebind input="kb1_lshift+np_add"/><rebind input="js2_button21"/></action>
  </actionmap>
 </ActionProfiles>
</ActionMaps>
""";

var tempRoot = Path.Combine(Path.GetTempPath(), "StarbindSmoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempRoot);
var file = Path.Combine(tempRoot, "layout_smoke_exported.xml");
File.WriteAllText(file, xml);

try
{
    var service = new StarbindProfileService();
    var detected = new[] { new InputDevice(1, "Microsoft PC-joystick driver", 32, 6), new InputDevice(2, "Microsoft PC-joystick driver", 32, 6) };
    var profile = service.Load(file, detected);
    Require(profile.ProfileName == "HOSAS Test", "Profile name was not parsed.");
    Require(profile.Devices.Any(x => x.Instance == 1 && x.ProductName.Contains("Gladiator EVO R")), "Right VKB name was not parsed from profile options.");
    Require(profile.Devices.Any(x => x.Instance == 2 && x.ProductName.Contains("Gladiator EVO L")), "Left VKB name was not parsed from profile options.");
    Require(profile.Actions.Count(x => x.ActionName == "foip_pushtotalk") == 2, "Multiple rebinds on one action were collapsed.");
    Require(profile.Actions.Single(x => x.ActionName == "moveforward").Input == StarbindInput.Unbound, "Placeholder joystick input was not normalized to Unbound.");
    Require(profile.Actions.Single(x => x.ActionName == "v_mining_throttle").Context == "Mining", "Mining context classification failed.");
    Require(profile.Actions.Single(x => x.ActionName == "jump").Context == "On Foot", "On Foot context classification failed.");
    Require(profile.Actions.Single(x => x.ActionName == "v_strafe_longitudinal").Intent == "Move Forward / Backward", "Movement intent classification failed.");
    Require(profile.Actions.Single(x => x.ActionName == "v_strafe_longitudinal").DisplayName == "Move Forward / Backward", "Friendly ship movement name was not used.");
    Require(profile.Actions.Single(x => x.ActionName == "vehicle_throttle_abs").Intent == "Move Forward / Backward", "Vehicle throttle intent classification failed.");
    Require(profile.Actions.Single(x => x.ActionName == "vehicle_throttle_abs").DisplayName == "Throttle - Forward / Backward", "Friendly vehicle throttle name was not used.");
    Require(profile.Actions.Single(x => x.ActionName == "moveforward").Behavior == "Axis", "On-foot movement was not classified as an axis-compatible action.");
    Require(profile.Actions.Single(x => x.ActionName == "moveforward").DisplayName == "Move Forward / Backward", "Friendly on-foot movement name was not used.");
    Require(profile.Actions.Single(x => x.ActionName == "turret_elevation").DisplayName == "Turret Elevation", "Friendly turret action name was not used.");
    Require(profile.Actions.Single(x => x.ActionName == "v_atc_loading_area_request").Attributes.TryGetValue("multiTap", out var multiTap) && multiTap == "2", "Rebind attributes were not preserved.");
    Require(profile.Actions.Single(x => x.ActionName == "v_operator_mode_cycle").Intent == "Operator Mode", "Operator Mode intent classification failed.");
    Require(profile.Actions.Single(x => x.ActionName == "v_master_mode_cycle").Intent == "Master Mode", "Master Mode intent classification failed.");
    Require(profile.Actions.Single(x => x.ActionName == "v_operator_mode_cycle").Intent != profile.Actions.Single(x => x.ActionName == "v_master_mode_cycle").Intent, "Operator Mode and Master Mode were merged.");
    Require(profile.Actions.Single(x => x.ActionName == "v_operator_mode_cycle").DisplayName == "Cycle Operator Mode", "Operator Mode display name is not explicit.");
    Require(profile.Actions.Single(x => x.ActionName == "v_master_mode_cycle").DisplayName == "Cycle Master Mode", "Master Mode display name is not explicit.");

    var hardware = new HardwareDefinitionService();
    var settings = new StarbindV5Settings();
    var rightStick = profile.Devices.Single(x => x.Instance == 1 && x.Kind == StarbindDeviceKind.Joystick);
    var rightTemplate = hardware.Resolve(rightStick, settings);
    Require(rightTemplate.Id == "vkb-gladiator", "VKB Gladiator hardware template was not selected.");
    var namedControls = hardware.BuildControls(rightStick, profile, settings);
    Require(namedControls.Any(x => x.Input == "js1_button1" && x.DisplayName == "Trigger"), "Named Trigger control was not created.");
    Require(namedControls.Any(x => x.Input == "js1_button3" && x.DisplayName == "Weapon 1"), "Named Weapon 1 control was not created.");
    Require(rightTemplate.Controls.Any(x => x.Group == "Encoders"), "Encoder options are missing from the VKB template.");
    Require(rightTemplate.Controls.Any(x => x.Group == "System Controls"), "System Controls are missing from the VKB template.");

    var atc = profile.Actions.Single(x => x.ActionName == "v_atc_loading_area_request");
    service.SaveAssignments(profile,
    [
        new BindingMutation(BindingMutationKind.RemoveInput, "Flight", string.Empty, -1, string.Empty, "js2_button4"),
        new BindingMutation(BindingMutationKind.AddInput, "Flight", atc.ActionMap, atc.ActionOrdinal, atc.ActionName, "js2_button4")
    ]);
    var preserved = service.Load(file, []);
    Require(preserved.Actions.Single(x => x.ActionName == "v_atc_loading_area_request").Attributes.TryGetValue("multiTap", out var savedMultiTap) && savedMultiTap == "2", "Keeping an existing action stripped its multiTap attribute.");
    Require(!preserved.Actions.Any(x => x.ActionName == "v_autoland" && x.Input == "js2_button4"), "Duplicate flight input was not removed.");

    var jump = preserved.Actions.Single(x => x.ActionName == "jump");
    var backup = service.SaveAssignments(preserved,
    [
        new BindingMutation(BindingMutationKind.RemoveInput, "On Foot", string.Empty, -1, string.Empty, "js2_button2"),
        new BindingMutation(BindingMutationKind.AddInput, "On Foot", jump.ActionMap, jump.ActionOrdinal, jump.ActionName, "js1_button9")
    ]);
    Require(File.Exists(backup), "Backup was not created.");
    var reloaded = service.Load(file, []);
    Require(reloaded.Actions.Any(x => x.ActionName == "jump" && x.Input == "js1_button9"), "New binding was not written.");
    Require(!reloaded.Actions.Any(x => x.Context == "On Foot" && x.Input == "js2_button2"), "Old state binding was not removed.");

    var flightMove = reloaded.Actions.Single(x => x.ActionName == "v_strafe_longitudinal");
    var onFootMove = reloaded.Actions.Single(x => x.ActionName == "moveforward");
    var plan = new ControlBindingPlan { Input = "js2_y", FriendlyName = "Y Axis", IsDirty = true };
    plan.States["Flight"] = new PlannedStateBinding
    {
        Context = "Flight",
        Choices = new[] { flightMove },
        Existing = reloaded.Actions.Where(x => x.Context == "Flight" && x.Input == "js2_y").ToList(),
        Enabled = true,
        Action = flightMove,
        Status = "BOUND"
    };
    plan.States["On Foot"] = new PlannedStateBinding
    {
        Context = "On Foot",
        Choices = new[] { onFootMove },
        Existing = [],
        Enabled = true,
        Action = onFootMove,
        Status = "PENDING"
    };
    var workspaceBackup = service.SaveWorkspace(reloaded, [plan], [new AxisTuningChange("js2_z", "VKBsim Gladiator EVO L", 2, "z", 0.05, 1.35, "Gentle")]);
    Require(File.Exists(workspaceBackup), "Workspace backup was not created.");
    var workspaceReloaded = service.Load(file, []);
    Require(workspaceReloaded.Actions.Any(x => x.ActionName == "v_strafe_longitudinal" && x.Input == "js2_y"), "Existing Flight state binding was not preserved by workspace save.");
    Require(workspaceReloaded.Actions.Any(x => x.ActionName == "moveforward" && x.Input == "js2_y"), "Cross-state On Foot binding was not added by workspace save.");
    var savedDocument = XDocument.Load(file);
    var zOption = savedDocument.Descendants().FirstOrDefault(x => x.Name.LocalName == "option" && (string?)x.Attribute("input") == "z" && x.Parent?.Name.LocalName == "deviceoptions" && ((string?)x.Parent.Attribute("name"))?.Contains("Gladiator EVO L") == true);
    Require(zOption is not null, "Axis tuning option was not created for the selected device.");
    Require((string?)zOption!.Attribute("deadzone") == "0.050000", "Axis deadzone was not persisted.");
    Require((string?)zOption.Attribute("exponent") == "1.350000", "Axis response curve exponent was not persisted.");

    Console.WriteLine("Starbind profile, hardware, action-language and workspace smoke tests passed.");
}
finally
{
    try { Directory.Delete(tempRoot, true); } catch { }
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
