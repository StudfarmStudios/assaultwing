xof 0303txt 0032
template KeyValuePair {
 <26e6b1c3-3d4d-4a1d-a437-b33668ffa1c2>
 STRING key;
 STRING value;
}

template Frame {
 <3d82ab46-62da-11cf-ab39-0020af71e433>
 [...]
}

template Matrix4x4 {
 <f6f23f45-7686-11cf-8f52-0040333594a3>
 array FLOAT matrix[16];
}

template FrameTransformMatrix {
 <f6f23f41-7686-11cf-8f52-0040333594a3>
 Matrix4x4 frameMatrix;
}

template ObjectMatrixComment {
 <95a48e28-7ef4-4419-a16a-ba9dbdf0d2bc>
 Matrix4x4 objectMatrix;
}

template Vector {
 <3d82ab5e-62da-11cf-ab39-0020af71e433>
 FLOAT x;
 FLOAT y;
 FLOAT z;
}

template MeshFace {
 <3d82ab5f-62da-11cf-ab39-0020af71e433>
 DWORD nFaceVertexIndices;
 array DWORD faceVertexIndices[nFaceVertexIndices];
}

template Mesh {
 <3d82ab44-62da-11cf-ab39-0020af71e433>
 DWORD nVertices;
 array Vector vertices[nVertices];
 DWORD nFaces;
 array MeshFace faces[nFaces];
 [...]
}

template MeshNormals {
 <f6f23f43-7686-11cf-8f52-0040333594a3>
 DWORD nNormals;
 array Vector normals[nNormals];
 DWORD nFaceNormals;
 array MeshFace faceNormals[nFaceNormals];
}

template Coords2d {
 <f6f23f44-7686-11cf-8f52-0040333594a3>
 FLOAT u;
 FLOAT v;
}

template MeshTextureCoords {
 <f6f23f40-7686-11cf-8f52-0040333594a3>
 DWORD nTextureCoords;
 array Coords2d textureCoords[nTextureCoords];
}

template ColorRGBA {
 <35ff44e0-6c7c-11cf-8f52-0040333594a3>
 FLOAT red;
 FLOAT green;
 FLOAT blue;
 FLOAT alpha;
}

template ColorRGB {
 <d3e16e81-7835-11cf-8f52-0040333594a3>
 FLOAT red;
 FLOAT green;
 FLOAT blue;
}

template Material {
 <3d82ab4d-62da-11cf-ab39-0020af71e433>
 ColorRGBA faceColor;
 FLOAT power;
 ColorRGB specularColor;
 ColorRGB emissiveColor;
 [...]
}

template MeshMaterialList {
 <f6f23f42-7686-11cf-8f52-0040333594a3>
 DWORD nMaterials;
 DWORD nFaceIndexes;
 array DWORD faceIndexes[nFaceIndexes];
 [Material <3d82ab4d-62da-11cf-ab39-0020af71e433>]
}

template TextureFilename {
 <a42790e1-7810-11cf-8f52-0040333594a3>
 STRING filename;
}


KeyValuePair {
 "Date";
 "2008-09-10 19:59:34";
}

KeyValuePair {
 "File";
 "E:\\Projects\\AW\\kudit\\bullets.max";
}

KeyValuePair {
 "User";
 "bluesun";
}

KeyValuePair {
 "CoreTime";
 "0";
}

Frame Bullet {
 

 FrameTransformMatrix relative {
  1.000000,0.000000,0.000000,0.000000,0.000000,0.000000,1.000000,0.000000,0.000000,-1.000000,0.000000,0.000000,0.000002,0.000002,-9.873155,1.000000;;
 }

 ObjectMatrixComment object {
  1.000000,0.000000,0.000000,0.000000,0.000000,0.000000,1.000000,0.000000,0.000000,-1.000000,0.000000,0.000000,-0.000000,7.984163,0.000000,1.000000;;
 }

 Mesh mesh_Bullet {
  13;
  -0.000001;-10.645549;0.000001;,
  -31.936653;10.645551;18.438633;,
  -0.000003;10.645551;36.877274;,
  -0.000001;-10.645549;0.000001;,
  -31.936651;10.645551;-18.438639;,
  -0.000001;-10.645549;0.000001;,
  0.000004;10.645551;-36.877266;,
  -0.000001;-10.645549;0.000001;,
  31.936655;10.645551;-18.438629;,
  -0.000001;-10.645549;0.000001;,
  31.936649;10.645551;18.438641;,
  -0.000001;-10.645549;0.000001;,
  -0.000003;10.645551;36.877274;;
  6;
  3;1,0,2;,
  3;4,3,1;,
  3;6,5,4;,
  3;8,7,6;,
  3;10,9,8;,
  3;12,11,10;;

  MeshNormals normals {
   13;
   0.000000;-1.000000;-0.000000;,
   -0.433013;-0.866025;0.250000;,
   -0.000000;-0.866025;0.500000;,
   0.000000;-1.000000;-0.000000;,
   -0.433013;-0.866025;-0.250000;,
   0.000000;-1.000000;-0.000000;,
   0.000000;-0.866025;-0.500000;,
   0.000000;-1.000000;-0.000000;,
   0.433013;-0.866025;-0.250000;,
   0.000000;-1.000000;-0.000000;,
   0.433013;-0.866025;0.250000;,
   0.000000;-1.000000;-0.000000;,
   -0.000000;-0.866025;0.500000;;
   6;
   3;1,0,2;,
   3;4,3,1;,
   3;6,5,4;,
   3;8,7,6;,
   3;10,9,8;,
   3;12,11,10;;
  }

  MeshTextureCoords tc0 {
   13;
   0.083333;1.000000;,
   0.166667;0.666667;,
   -0.000000;0.666667;,
   0.250000;1.000000;,
   0.333333;0.666667;,
   0.416667;1.000000;,
   0.500000;0.666667;,
   0.583333;1.000000;,
   0.666667;0.666667;,
   0.750000;1.000000;,
   0.833333;0.666667;,
   0.916667;1.000000;,
   1.000000;0.666667;;
  }

  MeshMaterialList mtls {
   1;
   6;
   0,
   0,
   0,
   0,
   0,
   0;

   Material medium {
    1.000000;1.000000;1.000000;1.000000;;
    9.999999;
    0.454902;0.454902;0.454902;;
    0.300000;0.300000;0.300000;;

    TextureFilename Diffuse {
     "bullet_medium.png";
    }
   }
  }
 }
}