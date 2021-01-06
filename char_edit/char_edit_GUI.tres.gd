extends Camera
var right_mouse_pressed = false
var midle_mouse_pressed = false
var speed_rot=0.005
var default_translation = Vector3()
# Called when the node enters the scene tree for the first time.
func _ready():
	default_translation = $spt.translation
	pass # Replace with function body.

func _input(event):
	if event is InputEventMouseButton:
		if event.button_index == BUTTON_RIGHT:
			if event.pressed:
				Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
				right_mouse_pressed = true
			else:
				Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
				right_mouse_pressed = false
		if event.button_index == BUTTON_MIDDLE:
			if event.pressed:
				midle_mouse_pressed = true
			else:
				midle_mouse_pressed = false
		if event.button_index == BUTTON_WHEEL_UP:
			$spt.translation.z += speed_rot*5
		if event.button_index == BUTTON_WHEEL_DOWN:
			$spt.translation.z -= speed_rot*5
	if event is InputEventMouseMotion:
		if midle_mouse_pressed:
			if (abs(event.relative.x) > abs(event.relative.y)):
				$spt.translation.x +=event.relative.x*speed_rot
			else:
				$spt.translation.y -=event.relative.y*speed_rot
		if right_mouse_pressed:
			if (abs(event.relative.x) > abs(event.relative.y)):
				$spt.rotate_y(event.relative.x*speed_rot)
			else:
				$spt.rotate_x(event.relative.y*speed_rot)



func _on_reset_view_pressed():
	$spt.translation = default_translation
	$spt.rotation_degrees = Vector3 (0,0,0)
