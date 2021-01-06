extends KinematicBody

var appearance ={}
func _ready():
	take_on_clothes("body")
	take_on_clothes("eyes")

func generate_mesh(mesh_inst):
	reset_mesh(mesh_inst)
	var vertex_arr=mesh_inst.mesh.surface_get_arrays(0)[Mesh.ARRAY_VERTEX]
	for shape_name in appearance:
		var shp_indx =char_edit_global.meshs_shapes[mesh_inst.name]["shp_name_index"]
		if shp_indx.keys().has(shape_name):
			shp_indx = shp_indx[shape_name]
			vertex_arr = update_vertex(mesh_inst,vertex_arr,shp_indx,appearance[shape_name])
	save_mesh(mesh_inst,vertex_arr)

func generate_all_meshs():
	var time= OS.get_ticks_msec ( )
	var meshs =$skeleton.get_children()
	for mesh_inst in meshs:
		generate_mesh(mesh_inst)
	print (OS.get_ticks_msec ( )-time)
	
func reset_mesh(mesh_inst):
	save_mesh(mesh_inst,char_edit_global.meshs_shapes[mesh_inst.name]["base_form"])

func update_vertex(mesh_inst,vertex_arr,shp_indx,value):
	var blend = char_edit_global.meshs_shapes[mesh_inst.name]["blendshapes"][shp_indx]
	for i in range(len(vertex_arr)):
		vertex_arr[i] += blend[i]*value
	return vertex_arr

func save_mesh(mesh_inst, vertex_arr):
	var mat = mesh_inst.get_surface_material(0)
	var mesh_arrs = mesh_inst.mesh.surface_get_arrays(0)
	mesh_arrs[Mesh.ARRAY_VERTEX] = vertex_arr
	mesh_inst.mesh = Mesh.new()
	mesh_inst.mesh.add_surface_from_arrays (Mesh.PRIMITIVE_TRIANGLES,mesh_arrs)
	mesh_inst.set_surface_material(0,mat)

func update_morph(shape_name,value):
	var temp = value;
	if appearance.has(shape_name):
		value -= appearance[shape_name]
	var meshs =$skeleton.get_children()
	for mesh_inst in meshs:
		var shp_indx =char_edit_global.meshs_shapes[mesh_inst.name]["shp_name_index"]
		if shp_indx.keys().has(shape_name):
			shp_indx = shp_indx[shape_name]
			var vertex_arr=mesh_inst.mesh.surface_get_arrays(0)[Mesh.ARRAY_VERTEX]
			save_mesh(mesh_inst,update_vertex(mesh_inst,vertex_arr,shp_indx,value))
	appearance[shape_name] = temp
	if appearance[shape_name]==0: #Удалим ключ, если он нулевой. Это позволит не крутить лишний раз цикл при генерации персонажа для этой формы
		appearance.erase(shape_name)

func random_face_gen(cartoonish):
	for param in char_edit_global.meshs_shapes["forms"]["head"].keys():
		appearance [param] = randf()*cartoonish
	generate_all_meshs()
		
func take_off_clothes(cloth):
	var clothes = $skeleton.get_children()
	for i in clothes:
		if cloth == i.name:
			i.queue_free()

func take_on_clothes(cloth):
	var take_cloth = $skeleton.get_children()
	for i in take_cloth:
		if cloth == i.name: # Проверка на то, что вещь уже одета. Иначе у нас снова будет такая же сцена, но её имя изменится, а у нас по имени загружается меш и тд. Короче, вылет.
			return
	take_cloth = load("res://char_edit/meshs/"+cloth+".tscn").instance()
	$skeleton.add_child(take_cloth)
	generate_mesh(take_cloth)
