extends Camera3D
class_name DebugCamera3D


@export_range(0, 10, 0.01) var sensitivity: float = 3
@export_range(0, 1000, 0.1) var default_velocity: float = 5
@export_range(0, 10, 0.01) var speed_scale: float = 1.17
@export_range(1, 100, 0.1) var boost_speed_multiplier: float = 3.0
@export var max_speed: float = 1000
@export var min_speed: float = 0.2

@onready var _velocity = default_velocity

var main_cam: Camera3D


func _ready() -> void:
	main_cam = get_viewport().get_camera_3d()
	process_mode = PROCESS_MODE_ALWAYS


func _process(delta: float) -> void:
	if !current:
		position = main_cam.global_position
		rotation = main_cam.global_rotation
		return
	
	var direction = Vector3(
		float(Input.is_physical_key_pressed(KEY_D)) - float(Input.is_physical_key_pressed(KEY_A)),
		float(Input.is_physical_key_pressed(KEY_E)) - float(Input.is_physical_key_pressed(KEY_Q)),
		float(Input.is_physical_key_pressed(KEY_S)) - float(Input.is_physical_key_pressed(KEY_W))
	).normalized()
	
	if Input.is_physical_key_pressed(KEY_SHIFT): # boost
		translate(direction * _velocity * delta * boost_speed_multiplier)
	else:
		translate(direction * _velocity * delta)


func _input(event: InputEvent) -> void:
	if Input.get_mouse_mode() == Input.MOUSE_MODE_CAPTURED:
		if event is InputEventMouseMotion:
			rotation.y -= event.relative.x / 1000 * sensitivity
			rotation.x -= event.relative.y / 1000 * sensitivity
			rotation.x = clamp(rotation.x, PI / -2, PI / 2)
	
	if event is InputEventMouseButton:
		match event.button_index:
			MOUSE_BUTTON_RIGHT:
				Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED if event.pressed else Input.MOUSE_MODE_VISIBLE)
			MOUSE_BUTTON_WHEEL_UP: # increase fly velocity
				_velocity = clamp(_velocity * speed_scale, min_speed, max_speed)
			MOUSE_BUTTON_WHEEL_DOWN: # decrease fly velocity
				_velocity = clamp(_velocity / speed_scale, min_speed, max_speed)
	
	# Toggle cameras and gizmos
	if event is InputEventKey && event.is_pressed():
		if event.keycode == KEY_MINUS:
			var cam := main_cam
			cam.current = !cam.current
			current = !cam.current
			
			DebugCam.gizmo_manager_3d.active = !DebugCam.gizmo_manager_3d.active
			DebugCam.gui_instance.visible = !DebugCam.gui_instance.visible
			get_tree().paused = !get_tree().paused
