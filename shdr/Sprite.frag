#version 330 core

// Force the bindless texture
// extension. It should wor
#extension GL_ARB_bindless_texture : require


// The texture coordinate
// of the current fragment
in vec2 texCoord;

// The color modifier
// of the current fragment
flat in vec4 color;

// The bindless texture
// of the current fragment
flat in uvec2 tex;


// Output color of
// the current fragment
out vec4 FragColor;


// Boolean to indicate
// if the transparent-
// pass is active noew
uniform bool uIsTransparentPass;


// Starting point of
// the fragment shader
void main()
{

    // Calculate how the pixel
    // will look with the given
    // color and texture sample
    vec4 currentColor =
        texture(sampler2D(tex), texCoord) * color;

    // If the pixel won't be
    // visible...
    if(currentColor.a == 0)
        // Discard this shader
        discard;

    // If it is the transparent pass
    // and the alpha of the color modifier
    // is 1...
    if(uIsTransparentPass && currentColor.a == 1)
        // Discard this shader
        discard;

    // If it isn't the transparent pass
    // and the alpha of the color modifier
    // is not 1...
    if(!uIsTransparentPass && currentColor.a != 1)
        // Discard this shader
        discard;

    FragColor = currentColor;
}