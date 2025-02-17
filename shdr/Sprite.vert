#version 330 core


// The position of the vertex
layout(location = 0) in vec3 vPosition; 

// The texture cooridnate of the vertex
layout(location = 1) in vec2 vTexCoord;

// The color modifier of the vertex
layout(location = 2) in vec4 vColor;

// The bindless texture of the vertex
layout(location = 3) in uvec2 vTex;

// The model matrix of the vertex
layout(location = 4) in mat4 vModel;


// Ouput the texture coordinate
// to the fragment shader
out vec2 texCoord;

// Ouput the color modifier
// to the fragment shader
flat out vec4 color;

// Ouput the bindless texture
// to the fragment shader
flat out uvec2 tex;


// View matrix of the
// current camera
uniform mat4 uView;

// Projection matrix of
// the current camera
uniform mat4 uProjection;


// Starting point
// of the shader
void main()
{
    // Calculate the position
    // of the 
    gl_Position = vec4(vPosition, 1.0) * vModel * uView * uProjection;

    // Set the output
    // texture coorinate
    texCoord = vTexCoord;

    // Set the ouput color
    color = vColor;

    // Set the output
    // bindless texture
    tex = vTex;
}