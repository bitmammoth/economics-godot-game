extends Control

var forms = {}
var character
var cartoonish =1.0;
func _ready():
	randomize()
	var Body_forms = {}
	var Head_forms = {}
	var Exp_forms = {}
	var morphs=$morphs/Body.get_children()
	for i in morphs:
		i.connect("change_morph", self, "change_morph") #подключаем сигналы со всех слайдеров
		Body_forms[i.text] = i.vertex_groups
	morphs=$morphs/Head.get_children()
	for i in morphs:
		i.connect("change_morph", self, "change_morph") #подключаем сигналы со всех слайдеров
		Head_forms[i.text] = i.vertex_groups
	morphs=$morphs/Face.get_children()
	for i in morphs:
		i.connect("change_morph", self, "change_morph") #подключаем сигналы со всех слайдеров
		Head_forms[i.text] = i.vertex_groups
	morphs=$morphs/Eyes.get_children()
	for i in morphs:
		i.connect("change_morph", self, "change_morph") #подключаем сигналы со всех слайдеров
		Head_forms[i.text] = i.vertex_groups
	morphs=$morphs/Exp.get_children()
	for i in morphs:
		i.connect("change_morph", self, "change_morph") #подключаем сигналы со всех слайдеров
		Exp_forms[i.text] = i.vertex_groups
	forms["body"] = Body_forms
	forms["head"] = Head_forms
	forms["exp"] = Exp_forms
	var path= OS.get_executable_path () # Здесь устанавливаем пути для открытия, сохранения файлов.
	path =path.substr(0,path.find_last("\\")+1)
	$save_file.current_dir = path
	$open_file.current_dir = path

func set_character(human):
	character = human
	$cam/spt.add_child(character)
	
func change_morph(text,value):
	character.update_morph(text,value)
	
func load_json (filepath):
	var file=File.new()
	file.open(filepath,File.READ)
	var json = parse_json(file.get_as_text())
	file.close()
	return json

func save_json(filepath, value):
	var file=File.new()
	file.open(filepath,File.WRITE)
	file.store_line(to_json(value))
	file.close()

func _on_save_pressed():
	$save_file.popup()
	$save_file.invalidate()

func _on_open_pressed():
	$open_file.popup()
	$open_file.invalidate()

func _on_save_file_file_selected(path):
	save_json(path,character.appearance)

func _on_open_file_file_selected(path):
	character.appearance=load_json(path)
	reset_all_sliders(character.appearance)
	character.generate_all_meshs()

func reset_tab_slider(morphs, appearance):
	for sld in morphs:
		if appearance.has(sld.text):
			sld.set_slider (appearance[sld.text])
		else:
			sld.set_slider(0)

func reset_all_sliders(appearance):
	reset_tab_slider($morphs/Body.get_children(), appearance)
	reset_tab_slider($morphs/Head.get_children(), appearance)
	reset_tab_slider($morphs/Face.get_children(), appearance)
	reset_tab_slider($morphs/Eyes.get_children(), appearance)
	reset_tab_slider($morphs/Exp.get_children(), appearance)
	
func set_chahracter(chr):
	character=chr
	reset_all_sliders(character.appearance)

func _on_random_gen_pressed():
	character.random_face_gen(cartoonish)
	reset_tab_slider($morphs/Head.get_children(),character.appearance)
	reset_tab_slider($morphs/Face.get_children(),character.appearance)
	reset_tab_slider($morphs/Eyes.get_children(),character.appearance)
	
func _on_print_pressed():
	$tips.text=str(character.appearance)

func set_cartoonish(value):
	cartoonish = value


func _on_dress_mini_03_toggled(button_pressed):
	if button_pressed:
		character.take_on_clothes("dress_mini_03")
	else:
		character.take_off_clothes("dress_mini_03")

func _on_male_shirt_01_toggled(button_pressed):
	if button_pressed:
		character.take_on_clothes("male_shirt_01")
	else:
		character.take_off_clothes("male_shirt_01")

func _on_male_jeans_01_toggled(button_pressed):
	if button_pressed:
		character.take_on_clothes("male_jeans_01")
	else:
		character.take_off_clothes("male_jeans_01")
