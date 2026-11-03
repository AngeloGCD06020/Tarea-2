# Tarea 08: Model Info

Escena: `Assets/Scenes/Tarea 08.unity`.

## Comportamiento

- Reconoce las tres imágenes de `Assets/Image Libraries/ImageLibraries.asset`: Greninja, Paletas y Miku.
- Cada imagen tiene un `ModelInfo` independiente en `Assets/Tarea08/ModelInfo/`, con nombre, descripción, categoría, nivel e icono editables desde el Inspector.
- Instancia el prefab correspondiente sobre cada imagen y conserva su orientación y escala originales.
- Un toque en Android o un clic en el Editor selecciona el modelo mediante `Physics.Raycast` sobre sus colliders. El panel lee los cinco datos del ScriptableObject del modelo seleccionado.
- Los toques sobre el panel y el encabezado se ignoran para la selección 3D. Tocar un espacio vacío restablece la ficha.
- Tanto `TrackingState.Limited` como `TrackingState.None` ocultan el modelo. Si era el seleccionado, todos los datos y el icono vuelven al estado original, aunque otras imágenes sigan detectadas.
- Al recuperar una imagen se reutiliza su instancia. Si AR Foundation retira el trackable, se elimina su modelo; una detección posterior puede volver a crearlo.

La escena y los scripts de la actividad anterior se conservan. La implementación de esta tarea está en `Assets/Scripts/Tarea08/`.

## Abrir y probar

1. Abrir este proyecto con **Unity 6000.3.14f1** y abrir **Tarea 08**.
2. Para verificar automáticamente la implementación: **Tareas > Tarea 08 > Verificar comportamiento**.
3. Para generar el APK: **Tareas > Tarea 08 > Generar APK Android**.
4. Instalar el APK en un Android compatible con ARCore, de 64 bits, Android 10 o posterior. Permitir el acceso a la cámara.
5. Mostrar ante la cámara las imágenes originales de `Assets/Image/` que utiliza la biblioteca. También hay copias para identificarlas en `Assets/Tarea08/Icons/`.
6. Detectar dos o tres imágenes, tocar distintos modelos y retirar la imagen seleccionada para comprobar el reinicio de la ficha.

## Entregables locales

Los archivos listos para revisar están en **[Entrega/Tarea08](Entrega/Tarea08/LEEME.md)**: APK, video de editor e informes. Las herramientas, cuadros intermedios y registros completos permanecen en `Builds/Tarea08/`, excluida de Git.

- `Tarea08-ModelInfo.apk`: aplicación Android con esta escena como inicio.
- `Tarea08-Demostracion-Editor.mp4`: demostración automatizada del render, la UI y el raycast reales de Unity, con eventos de seguimiento **simulados**. El propio video identifica esta limitación; no sustituye una prueba de cámara/ARCore en un teléfono.
- `verificacion.json`: resultados de las comprobaciones de comportamiento.
- `demo-editor.json`: secuencia y estados comprobados durante la demostración.
- `Escena-original-antes-de-Tarea08.unity`: copia de la escena vacía que ya estaba guardada antes de implementar la actividad.

No había ningún teléfono conectado durante la preparación. Para la entrega que solicite una grabación de AR real, grabar la aplicación en el teléfono siguiendo el procedimiento anterior.

Se aprobaron **33 comprobaciones** en Unity, incluidas la selección mediante Physics.Raycast de los tres prefabs, el cambio de los cinco datos y la restauración completa al perder o retirar el modelo seleccionado. El APK pasó la verificación de firma de Android y la comprobación de integridad ZIP; incluye ARM64 y la escena Tarea 08.

El proceso interno de empaquetado de Unity presentó `Unable to establish loopback connection` en Java. El APK entregado se empaquetó correctamente ejecutando Gradle sobre el proyecto Android generado por Unity. El informe de compilación distingue ambas fases. Este problema de la herramienta local no se incorporó al código de la aplicación.

## Repositorio y entrega

Repositorio configurado en este proyecto: **shinjekyag/Repositorio**.

La actividad también solicita escribir el nombre del repositorio como comentario y adjuntar el APK y un video en la plataforma escolar. Este archivo documenta qué entregar; no realiza envíos ni publica comentarios en esa plataforma.

## Referencias técnicas

- [Ejemplo oficial de seguimiento de imágenes de Unity](https://github.com/Unity-Technologies/arfoundation-samples/blob/main/Assets/Scenes/ImageTracking/BasicImageTracking/TrackedImageInfoManager.cs).
- [ScriptableObjects](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ScriptableObject.html).
- [Physics.Raycast](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.Raycast.html).
