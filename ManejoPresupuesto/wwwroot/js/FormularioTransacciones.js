function inicializarFormularioTransacciones(urlObtenerCategorias) {
    $("#TipoOperacionId").change(async function () { //seleccionar el elemento HTML, para ver que tipoOperacion se seleccionara
        const valorSeleccionado = $(this).val(); //obtenemos el valor seleccionado y se asigna a la const ValorSel..

        const respuesta = await fetch(urlObtenerCategorias, { //fetch para consumir las categorias correspondientes al TipoOper selec
            method: 'POST',
            body: valorSeleccionado, //el valor seleccionado sera el cuerpo
            headers: {
                'Content-Type': 'application/json' //indica que el body request es en formato JSON
            }
        });
        const json = await respuesta.json();
        const opciones = json.map(categoria => //.map genera un arreglo de elementos a los que se les va aplicar la misma funcion
            //al valor le asignamos el valor de cada categoria,y se imprime su texto correspondiente
            `<option value=${categoria.value}>${categoria.text}</option>` /*generamos un arreglo de opciones en HTML*/
        );
        $("#CategoriaId").html(opciones); /*insertamos el arreglo de opciones en el elemento */

    })
}