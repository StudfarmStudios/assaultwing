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
 "2010-10-03 16:17:09";
}

KeyValuePair {
 "File";
 "";
}

KeyValuePair {
 "User";
 "bluesun";
}

KeyValuePair {
 "CoreTime";
 "0";
}

Frame Cylinder01 {
 

 FrameTransformMatrix relative {
  -0.000000,0.000000,-0.331961,0.000000,-0.000000,0.331961,0.000000,0.000000,0.331961,0.000000,-0.000000,0.000000,0.000000,0.000000,-9.099081,1.000000;;
 }

 ObjectMatrixComment object {
  1.000000,0.000000,0.000000,0.000000,0.000000,1.000000,0.000000,0.000000,-0.000000,-0.000000,1.000000,0.000000,-2.257744,0.000000,44.070080,1.000000;;
 }

 Mesh mesh_Cylinder01 {
  17;
  -9.030975;0.000002;176.280319;,
  -27.092930;31.284220;176.280319;,
  27.092930;0.000002;176.280319;,
  -27.092924;-31.284222;176.280319;,
  27.092930;0.000002;176.280319;,
  -27.092930;31.284220;0.000000;,
  27.092932;0.000002;0.000000;,
  -27.092930;31.284220;176.280319;,
  -27.092924;-31.284220;0.000000;,
  -27.092924;-31.284222;176.280319;,
  -27.092930;31.284222;-176.280319;,
  27.092928;0.000002;-176.280319;,
  -27.092924;-31.284220;-176.280319;,
  -9.030976;0.000002;-176.280319;,
  27.092928;0.000002;-176.280319;,
  -27.092930;31.284222;-176.280319;,
  -27.092924;-31.284220;-176.280319;;
  18;
  3;1,0,2;,
  3;3,0,1;,
  3;2,0,3;,
  3;5,4,6;,
  3;7,4,5;,
  3;8,7,5;,
  3;9,7,8;,
  3;6,9,8;,
  3;4,9,6;,
  3;10,6,11;,
  3;5,6,10;,
  3;12,5,10;,
  3;8,5,12;,
  3;11,8,12;,
  3;6,8,11;,
  3;14,13,15;,
  3;15,13,16;,
  3;16,13,14;;

  MeshNormals normals {
   17;
   -0.000000;-0.000000;3.012400;,
   -0.000000;-0.000000;3.012400;,
   -0.000000;-0.000000;3.012400;,
   -0.000000;-0.000000;3.012400;,
   3.012400;0.000000;0.000000;,
   -1.506200;2.608815;0.000000;,
   3.012400;0.000000;0.000000;,
   -1.506200;2.608815;0.000000;,
   -1.506200;-2.608815;-0.000000;,
   -1.506200;-2.608815;-0.000000;,
   -1.506200;2.608815;0.000000;,
   3.012400;0.000000;0.000000;,
   -1.506200;-2.608815;-0.000000;,
   0.000000;0.000000;-3.012400;,
   0.000000;0.000000;-3.012400;,
   0.000000;0.000000;-3.012400;,
   0.000000;0.000000;-3.012400;;
   18;
   3;1,0,2;,
   3;3,0,1;,
   3;2,0,3;,
   3;5,4,6;,
   3;7,4,5;,
   3;8,7,5;,
   3;9,7,8;,
   3;6,9,8;,
   3;4,9,6;,
   3;10,6,11;,
   3;5,6,10;,
   3;12,5,10;,
   3;8,5,12;,
   3;11,8,12;,
   3;6,8,11;,
   3;14,13,15;,
   3;15,13,16;,
   3;16,13,14;;
  }

  MeshTextureCoords tc0 {
   17;
   0.999501;0.500000;,
   0.999501;0.000500;,
   0.999500;0.500000;,
   0.999501;0.999501;,
   0.999500;0.500000;,
   0.500000;0.000500;,
   0.500000;0.500000;,
   0.999501;0.000500;,
   0.500000;0.999500;,
   0.999501;0.999501;,
   0.000500;0.000499;,
   0.000500;0.500000;,
   0.000500;0.999500;,
   0.000500;0.500000;,
   0.000500;0.500000;,
   0.000500;0.000499;,
   0.000500;0.999500;;
  }

  MeshMaterialList mtls {
   1;
   18;
   0,
   0,
   0,
   0,
   0,
   0,
   0,
   0,
   0,
   0,
   0,
   0,
   0,
   0,
   0,
   0,
   0,
   0;

   Material _01___Default {
    1.000000;1.000000;1.000000;1.000000;;
    28.000000;
    0.320000;0.320000;0.320000;;
    0.000000;0.000000;0.000000;;

    TextureFilename Diffuse {
     "bloodbowl_metalbar.png";
    }
   }
  }
 }
}