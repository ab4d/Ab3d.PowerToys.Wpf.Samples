// ----------------------------------------------------------------
// <copyright file="Wpf3DFile.cs" company="AB4D d.o.o.">
//     Copyright (c) AB4D d.o.o.  All Rights Reserved
// </copyright>
// ----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using System.IO;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Collections;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Resources;
using System.Windows.Documents;
using System.Windows.Markup;
using Exception = System.Exception;

namespace Ab3d.Utilities
{
    // Please send improvements to Wpf3DFile class to support@ab4d.com or create a pull request on https://github.com/ab4d/Ab3d.PowerToys.Wpf.Sample.

    // wpf3d is custom file format that can be used to save or read WPF 3D scene.
    // It uses chunks of data. Each chunk starts with ChunkType and chunk length (chunk length does not include the length of the chunk).
    // Chunks can be nested - for example DiffuseMaterial chunk can contain SolidColorBrush chunk - the length of parent DiffuseMaterial chunk must include the lengths of all child chunks.

    // wpf3d v1.0 features:
    // - FileHeader information (can be read with ReadHeader): Description, Comment, FileFormatVersion, ObjectsCount, DataPrecision, Camera, Thumbnail
    // - read / write hierarchical organization of Model3D objects (Model3DGroup and GeometryModel3D)
    // - read / write name of Model3D object (name is set by SetName method and also written into namedObjects dictionary)
    // - read / write Model3D.Transform - support all WPF transformation types: Transform3DGroup, MatrixTransform3D, TranslateTransform3D, ScaleTransform3D, RotateTransform3D (with AxisAngleRotation3D and QuaternionRotation3D)
    // - read / write MeshGeometry3D: Positions, Normals, TextureCoordinates, TriangleIndices and also PolygonIndices and EdgeLineIndices (later two is set by Ab3d.PowerToys).
    // - read / write DiffuseMaterial with SolidColorBrush or ImageBrush (relative path to texture file name is saved)
    // - read / write SpecularMaterial and MaterialGroup
    // - read / write WPF or Ab3d.PowerToys camera - the camera is serialized to xaml with XamlWriter and then read back with XamlReader.

    // TODO: 
    // - Serialize lights
    // - Serialize ModelVisual3D objects
    // - Serialize Visual3D objects from Ab3d.PowerToys library

    /// <summary>
    /// Wpf3DFile class defines the structure of the wpf3d file format and provides methods to read and write to that file.
    /// </summary>
    public class Wpf3DFile
    {
        /// <summary>
        /// When IsLogging is set to true (false by default), then log information is written to System.Diagnostics.Debug
        /// </summary>
        public static bool IsLogging = false;


        // Major and minor version of this file format (version is written to file header)
        private const int MajorVersion = 1; // can be from 0 to 9
        private const int MinorVersion = 0; // can be from 0 to 9


        private Dictionary<object, string> _objectNames;
        private Dictionary<string, object> _namedObjects;
        
        private Dictionary<MeshGeometry3D, int> _meshIndexes;
        private Dictionary<Material, int> _materialIndexes;

        private List<Model3D> _objects; // This is used for object references - index is object id
        private MeshGeometry3D[] _meshes;
        private Material[] _materials;

        private int _currentObjectIndex;
        private int _currentMeshIndex;
        private int _currentMaterialIndex;

        /// <summary>
        /// DataPrecisionType defines the possible data precision types
        /// </summary>
        public enum DataPrecisionType
        {
            /// <summary>
            /// Values for positions, normals and texture coordinates are stored as double (64 bit for one value).
            /// </summary>
            Double = 1,

            /// <summary>
            /// Values for positions, normals and texture coordinates are stored as float (32 bit for one value).
            /// </summary>
            Float,
        }



        /// <summary>
        /// FileFormatVersion defines the wpf3d file format version that was used in the read or written wpf3d file.
        /// </summary>
        public Version FileFormatVersion { get; private set; }

        /// <summary>
        /// DataPrecision defines the DataPrecisionType that was used in the read or written wpf3d file.
        /// </summary>
        public DataPrecisionType DataPrecision { get; private set; }


        
        private int _modelsCount;

        /// <summary>
        /// GeometryModelsCount defines the number of Model3D objects (GeometryModel3D and Model3DGroup) in the read or written wpf3d file.
        /// </summary>
        public int ModelsCount
        {
            get { return _modelsCount; }
        }


        private int _meshesCount;
        
        /// <summary>
        /// MeshesCount defines the number of MeshGeometry3D objects in the read or written wpf3d file.
        /// </summary>
        public int MeshesCount
        {
            get { return _meshesCount; }
        }


        private int _materialsCount;

        /// <summary>
        /// MaterialsCount defines the number of Material objects in the read or written wpf3d file.
        /// </summary>
        public int MaterialsCount
        {
            get { return _materialsCount; }
        }
        
        
        private long _totalPositionsCount;
        
        /// <summary>
        /// TotalPositionsCount defines the number of positions (Point3D) in the read or written wpf3d file.
        /// </summary>
        public long TotalPositionsCount
        {
            get { return _totalPositionsCount; }
        }


        private long _totalTriangleIndicesCount;

        /// <summary>
        /// TotalTriangleIndices defines the number of triangle indices in the read or written wpf3d file (note that number of triangles is TotalTriangleIndices divided by 3).
        /// </summary>
        public long TotalTriangleIndices
        {
            get { return _totalTriangleIndicesCount; }
        }

        /// <summary>
        /// Camera defines the Camera object (can be WPF or Ab3d.PowerToys camera).
        /// </summary>
        public object Camera { get; private set; }


        /// <summary>
        /// Gets or sets a description of the file.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a comment can be used to add any additional text that is not part of the header and description.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets a bitmap that represents a thumbnail and is written to the file header.
        /// </summary>
        public BitmapSource Thumbnail { get; set; }

        /// <summary>
        /// SourceFileName is set when reading the file.
        /// When writing the file it can be set by the user to make the textures part relative to the path of the SourceFileName.
        /// </summary>
        public string SourceFileName { get; set; }


        /// <summary>
        /// Gets or sets a Boolean that specifies if Normals are written to the wpf3d file.
        /// By default SaveNormals is set to true, but can be set to false to reduce file size and the calculate normals after reading the file (this can also be automatically done by WPF 3D).
        /// </summary>
        public bool SaveNormals { get; set; }

        /// <summary>
        /// Gets or sets a Boolean that specifies if TextureCoordinates are written to the wpf3d file. Texture coordinates are needed when 3D models is showing texture.
        /// When 3D model is not showing textures then this property can be set to false to reduce file size.
        /// </summary>
        public bool SaveTextureCoordinates { get; set; }


        /// <summary>
        /// Gets a dictionary where key is set to object name and value is set to object.
        /// This way it is possible to access the read object by its name.
        /// </summary>
        public Dictionary<string, object> NamedObjects
        {
            get { return _namedObjects; }
        }

        /// <summary>
        /// ResolveTextureFileName is a Func that can be used to resolve texture file names.
        /// It takes a texture file name as it is written in the wpf3d file and can return the actual file name with full path on the local disk.
        /// </summary>
        public Func<string, string> ResolveTextureFileName;


        /// <summary>
        /// Constructor
        /// </summary>
        public Wpf3DFile()
        {
            FileFormatVersion = new Version(MajorVersion, MinorVersion);
            DataPrecision     = DataPrecisionType.Float;

            SaveNormals = true;
            SaveTextureCoordinates = true;
        }

        /// <summary>
        /// WriteFile writes the rootModel (Model3DGroup or GeometryModel3D) to the specified fileName.
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <param name="rootModel">Model3DGroup or GeometryModel3D that will be written</param>
        /// <param name="camera">optional WPF or Ab3d.PowerToys camera (serialized as xaml inside the file)</param>
        /// <param name="dataPrecision">data precision (Float by default)</param>
        public void WriteFile(string fileName, Model3D rootModel, Camera camera = null, DataPrecisionType dataPrecision = DataPrecisionType.Float)
        {
            WriteFile(fileName, rootModel, namedObjects: null, camera: camera, dataPrecision: dataPrecision);
        }

        /// <summary>
        /// WriteFile writes the rootModel (Model3DGroup or GeometryModel3D) to the specified fileName.
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <param name="rootModel">Model3DGroup or GeometryModel3D that will be written</param>
        /// <param name="namedObjects">dictionary with object name as key and object as value</param>
        /// <param name="camera">optional WPF or Ab3d.PowerToys camera (serialized as xaml inside the file)</param>
        /// <param name="dataPrecision">data precision (Float by default)</param>
        public void WriteFile(string fileName, Model3D rootModel, Dictionary<string, object> namedObjects, object camera = null, DataPrecisionType dataPrecision = DataPrecisionType.Float)
        {
            Log("WriteFile: " + fileName);

            _namedObjects = namedObjects;

            using (var outputStream = File.OpenWrite(fileName))
            {
                WriteFile(outputStream, rootModel, namedObjects, camera, dataPrecision);
            }
        }

        /// <summary>
        /// WriteFile writes the rootModel (Model3DGroup or GeometryModel3D) to the specified fileName.
        /// </summary>
        /// <param name="outputStream">output stream</param>
        /// <param name="rootModel">Model3DGroup or GeometryModel3D that will be written</param>
        /// <param name="camera">optional WPF or Ab3d.PowerToys camera (serialized as xaml inside the file)</param>
        /// <param name="dataPrecision">data precision (Float by default)</param>
        public void WriteFile(Stream outputStream, Model3D rootModel, object camera = null, DataPrecisionType dataPrecision = DataPrecisionType.Float)
        {
            WriteFile(outputStream, rootModel, null, camera, dataPrecision);
        }

        /// <summary>
        /// WriteFile writes the rootModel (Model3DGroup or GeometryModel3D) to the specified fileName.
        /// </summary>
        /// <param name="outputStream">output stream</param>
        /// <param name="rootModel">Model3DGroup or GeometryModel3D that will be written</param>
        /// <param name="namedObjects">optional dictionary with object name as key and object as value</param>
        /// <param name="camera">optional WPF or Ab3d.PowerToys camera (serialized as xaml inside the file)</param>
        /// <param name="dataPrecision">data precision (Float by default)</param>
        public void WriteFile(Stream outputStream, Model3D rootModel, Dictionary<string, object> namedObjects, object camera = null, DataPrecisionType dataPrecision = DataPrecisionType.Float)
        {
            DataPrecision = dataPrecision;

            _namedObjects = namedObjects;

            using (BinaryWriter writer = new BinaryWriter(outputStream))
            {
                Serialize(rootModel, camera, writer);
            }
        }

        /// <summary>
        /// ReadFileHeader reads only the wpf3d file header and sets the properties with values from the file.
        /// </summary>
        /// <param name="fileName">file name</param>
        public void ReadFileHeader(string fileName)
        {
            ReadFile(fileName, readOnlyHeader: true);
        }

        /// <summary>
        /// ReadFileHeader reads only the wpf3d file header and sets the properties with values from the file.
        /// </summary>
        /// <param name="readStream">file stream</param>
        public void ReadFileHeader(Stream readStream)
        {
            ReadFile(readStream, readOnlyHeader: true);
        }

        /// <summary>
        /// ReadFile reads the specified wpf3d file and returns the 3D models as Model3DGroup or GeometryModel3D.
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <returns>3D models as Model3DGroup or GeometryModel3D</returns>
        public Model3D ReadFile(string fileName)
        {
            return ReadFile(fileName, readOnlyHeader: false);
        }

        /// <summary>
        /// ReadFile reads the specified wpf3d file and returns the 3D models as Model3DGroup or GeometryModel3D.
        /// </summary>
        /// <param name="readStream">file stream</param>
        /// <returns>3D models as Model3DGroup or GeometryModel3D</returns>
        public Model3D ReadFile(Stream readStream)
        {
            Model3D model;
            using (BinaryReader reader = new BinaryReader(readStream))
            {
                model = Deserialize(reader, readOnlyHeader: false);
            }

            return model;
        }
        
        private Model3D ReadFile(string fileName, bool readOnlyHeader)
        {
            Log("ReadFile: " + fileName);

            SourceFileName = fileName;

            Model3D model;
            using (var readStream = File.OpenRead(fileName))
            { 
                model = ReadFile(readStream, readOnlyHeader: false);
            }

            return model;
        }

        private Model3D ReadFile(Stream readStream, bool readOnlyHeader)
        {
            Model3D model;
            using (BinaryReader reader = new BinaryReader(readStream))
            {
                model = Deserialize(reader, readOnlyHeader: false);
            }

            return model;
        }


        private void Serialize(Model3D model, object camera, BinaryWriter writer)
        {
            if (model == null)
                return;

            this.Camera = camera;


            // Collect meshes, materials and Model3D objects
            CollectObjects(model);

            // Convert namedObjects into _objectNames
            if (_namedObjects != null)
            {
                _objectNames = new Dictionary<object, string>();

                foreach (KeyValuePair<string, object> onePair in _namedObjects)
                {
                    if (onePair.Value == null || _objectNames.ContainsKey(onePair.Value))
                        continue;

                    _objectNames.Add(onePair.Value, onePair.Key);
                }
            }
            else
            {
                _objectNames = null;
            }

            _currentObjectIndex = -1;


            //
            // Write header
            //

            // File starts with "WPF3DvX.Y:" where X and Y are major and minor versions
            WriteAsciiStringWithoutHeader("WPF3D", writer); 

            SerializeHeader(writer);


            //
            // Write data
            //

            if (Camera != null)
                SerializeCamera(Camera, writer);

            // First serialize all meshes
            for (int i = 0; i < _meshesCount; i++)
                SerializeMeshGeometry3D(_meshes[i], writer);

            // Then serialize all materials
            for (int i = 0; i < _materialsCount; i++)
                SerializeMaterial(_materials[i], writer);

            // Finally serialize all Model3D objects
            SerializeModel3D(model, -1, writer);


            //
            // Finish with EndOfData chunk
            //

            Chunk.BeginWriteChunk(ObjectTypes.EndOfData, 0, writer);

            // allow to free resources in GC
            _objects   = null;
            _meshes    = null;
            _materials = null;

            _meshIndexes     = null;
            _materialIndexes = null;
            _namedObjects    = null;
        }
        
        // if readOnlyHeader is true, then no model is read only header with thumbnail is read so reading is very fast
        private Model3D Deserialize(BinaryReader reader, bool readOnlyHeader)
        {
            string magic = ReadAsciiStringWithoutHeader(5, reader);
            if (magic != "WPF3D")
                throw new FileFormatException("Unknown file format!");


            Thumbnail = null;
            Comment   = null;
            Camera    = null;

            DeserializeHeader(reader);

            if (DataPrecision != DataPrecisionType.Double &&
                DataPrecision != DataPrecisionType.Float)
            {
                throw new NotSupportedException("The DataPrecision type in the file is not supported: " + ((int)DataPrecision).ToString());
            }


            if (readOnlyHeader)
            {
                _namedObjects = null;
                return null;
            }


            _namedObjects = new Dictionary<string, object>();

            if (_modelsCount > 0)
                _objects = new List<Model3D>(_modelsCount);

            if (_meshesCount > 0)
                _meshes = new MeshGeometry3D[_meshesCount];

            if (_materialsCount > 0)
                _materials = new Material[_materialsCount];

            _currentMeshIndex     = 0;
            _currentMaterialIndex = 0;

            bool isEndOfData = false;


            // Read data chunks
            do
            {
                Chunk oneRootChunk = Chunk.ReadChunkHeader(reader);

                if (oneRootChunk.Type == ObjectTypes.EndOfData)
                {
                    isEndOfData = true;
                }
                else if (oneRootChunk.Type == ObjectTypes.MeshGeometry3D)
                {
                    var mesh = DeserializeMeshGeometry3D(oneRootChunk);
                    _meshes[_currentMeshIndex] = mesh;
                    _currentMeshIndex++;
                }
                else if (oneRootChunk.Type == ObjectTypes.DiffuseMaterial || oneRootChunk.Type == ObjectTypes.SpecularMaterial || oneRootChunk.Type == ObjectTypes.MaterialGroup)
                {
                    var material = DeserializeMaterial(oneRootChunk);
                    _materials[_currentMaterialIndex] = material;
                    _currentMaterialIndex++;
                }
                else if (oneRootChunk.Type == ObjectTypes.GeometryModel3D)
                {
                    DeserializeGeometryModel3D(oneRootChunk);
                }
                else if (oneRootChunk.Type == ObjectTypes.Model3DGroup)
                {
                    DeserializeModel3DGroup(oneRootChunk);
                }
                else if (oneRootChunk.Type == ObjectTypes.Model3DGroup)
                {
                    DeserializeModel3DGroup(oneRootChunk);
                }
                else if (oneRootChunk.Type == ObjectTypes.CameraXaml)
                {
                    Camera = DeserializeCamera(oneRootChunk);
                }
                else
                {
#if DEBUG
                    throw new FileFormatException("Unknown chunk type in file data section: " + oneRootChunk.Type.ToString());
#else
                    // skip unknown chunk
#endif
                }

                // Skip unknown chunk or skip unknown data at the end of the chunk
                oneRootChunk.EndReadChunk();

            } while (!isEndOfData);


            if (!isEndOfData)
                throw new FileFormatException("Invalid file content");

            if (_currentMeshIndex != _meshesCount)
                throw new FileFormatException("Invalid number of meshes read");

            if (_currentMaterialIndex != _materialsCount)
                throw new FileFormatException("Invalid number of meshes read");


            Model3D rootModel;

            if (_objects.Count > 0)
                rootModel = _objects[0];
            else
                rootModel = null;


            // allow to free resources in GC
            _objects   = null;
            _meshes    = null;
            _materials = null;

            _namedObjects = null;

            return rootModel;
        }


        private void SerializeHeader(BinaryWriter writer)
        {
            int descriptionSize = GetSizeOfUnicodeString(this.Description); // Description is always written

            int commentSize = string.IsNullOrEmpty(this.Comment) ? 0 : Chunk.SizeOfChunkHeader + GetSizeOfUnicodeString(this.Comment); // Comment is written only when set

            byte[] bitmapBytes = GetBitmapBytes(this.Thumbnail);
            int thumbnailSize = bitmapBytes == null ? 0 : Chunk.SizeOfChunkHeader + 4 * SizeOfInt32 + bitmapBytes.Length;
            
            int chunkSize = 5 // version "vX.Y:" 
                            + 4 * SizeOfInt32 + 2 * SizeOfInt64 // counts
                            + descriptionSize + commentSize + thumbnailSize;


            Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.Header, chunkSize, writer);
            

            // Write version as "vX.Y:" where x and y are major and minor versions (only numbers from 0 to 9 are allowed)
            if (this.FileFormatVersion.Major > 9 || this.FileFormatVersion.Minor > 9)
                throw new Exception("Major and Minor version numbers can be only from 0 to 9");

            writer.Write((byte)'v');
            writer.Write((byte)(MajorVersion + '0')); // major version in ASCII
            writer.Write((byte)'.');
            writer.Write((byte)(MinorVersion + '0')); // minor version in ASCII
            writer.Write((byte)':');

            writer.Write((int)DataPrecision);

            writer.Write(_meshesCount);
            writer.Write(_materialsCount);
            writer.Write(_modelsCount);
            writer.Write(_totalPositionsCount);       // _totalPositionsCount is long
            writer.Write(_totalTriangleIndicesCount); // _totalTriangleIndicesCount is long

            string description = this.Description;
            if (description == null)
                description = "";

            WriteUnicodeString(description, writer);


            if (bitmapBytes != null)
            {
                int width        = this.Thumbnail.PixelWidth;
                int height       = this.Thumbnail.PixelHeight;
                int bitsPerPixel = this.Thumbnail.Format.BitsPerPixel;

                SerializeBitmap(bitmapBytes, width, height, bitsPerPixel, writer);
            }

            if (!string.IsNullOrEmpty(Comment))
                SerializeCommentText(writer, Comment);


            chunk.EndWriteChunk();
        }

        private void DeserializeHeader(BinaryReader reader)
        {
            Chunk chunk = Chunk.ReadChunkHeader(reader);

            reader.ReadByte(); // read 'v'
            byte majorVersion = reader.ReadByte();
            reader.ReadByte(); // read '.'
            byte minorVersion = reader.ReadByte();
            byte endOfVersionChar = reader.ReadByte(); // read ':'

            if (endOfVersionChar != ':')
                throw new Exception("Invalid version");

            this.FileFormatVersion = new Version((int)majorVersion - '0', (int)minorVersion - '0');

            if (this.FileFormatVersion.Major > MajorVersion ||
                (this.FileFormatVersion.Major == MajorVersion && this.FileFormatVersion.Minor > MinorVersion))
            {
                throw new Exception(string.Format("Cannot read file with higher version as the wpf3d reader version (file version: {0}.{1}; reader version {2}.{3}",
                                                  this.FileFormatVersion.Major, this.FileFormatVersion.Minor, MajorVersion, MinorVersion));
            }

            this.DataPrecision = (DataPrecisionType)reader.ReadInt32();

            _meshesCount               = reader.ReadInt32();
            _materialsCount            = reader.ReadInt32();
            _modelsCount               = reader.ReadInt32();
            _totalPositionsCount       = reader.ReadInt64();
            _totalTriangleIndicesCount = reader.ReadInt64();

            this.Description = ReadUnicodeString(reader);


            // Read any other optional header chunk

            while (reader.BaseStream.Position < chunk.DataEndPosition)
            {
                Chunk oneRootChunk = Chunk.ReadChunkHeader(reader);

                if (oneRootChunk.Type == ObjectTypes.Bitmap)
                {
                    Thumbnail = DeserializeBitmap(oneRootChunk);
                }
                else if (oneRootChunk.Type == ObjectTypes.Comment)
                {
                    Comment = DeserializeCommentText(oneRootChunk);
                }
                else
                {
#if DEBUG
                    throw new FileFormatException("Invalid chunk type in file header: " + oneRootChunk.Type.ToString());
#else
                    // skip unknown chunk
#endif
                }
            }

            chunk.EndReadChunk();
        }

        private void SerializeCommentText(BinaryWriter writer, string text)
        {
            if (text == null)
                text = "";

            int textSize = GetSizeOfUnicodeString(text);

            Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.Comment, textSize, writer);

            WriteUnicodeString(text, writer);

            chunk.EndWriteChunk();
        }

        private string DeserializeCommentText(BinaryReader reader)
        {
            Chunk chunk = Chunk.ReadChunkHeader(reader);
            string text = DeserializeCommentText(chunk);

            return text;
        }
        
        private string DeserializeCommentText(Chunk chunk)
        {
            string text;
            if (chunk.Type == ObjectTypes.Comment && chunk.ChunkLength > 0)
                text = ReadUnicodeString(chunk.Reader);
            else
                text = null;

            chunk.EndReadChunk();

            return text;
        }

        #region DeSerialization
        private Model3D DeserializeModel3D(Chunk chunk)
        {
            Model3D model;

            if (chunk.Type == ObjectTypes.GeometryModel3D)
            {
                model = DeserializeGeometryModel3D(chunk);
            }
            else if (chunk.Type == ObjectTypes.Model3DGroup)
            {
                model = DeserializeModel3DGroup(chunk);
            }
            else if (chunk.Type == ObjectTypes.Null || chunk.Type == ObjectTypes.Unknown)
            {
                model = null;
            }
            else
            {
#if DEBUG
                throw new NotSupportedException("Not supported Model3D type");
#else
                model = null;

                // Skip unknown model
                chunk.EndReadChunk();
#endif
            }

            return model;
        }

        private Model3DGroup DeserializeModel3DGroup(Chunk chunk)
        {
            BinaryReader reader = chunk.Reader;

            var name = ReadUnicodeString(reader);

            var transform = DeserializeTransform(reader);
            
            int childrenCount = reader.ReadInt32();
            int parentIndex = reader.ReadInt32();

            if (IsLogging)
                Log(string.Format("     Model3DGroup name: {0}, Index: {1}; children count: {2}; Parent index: {3}", name, _objects.Count, childrenCount, parentIndex));

            Rect3D bounds = ReadRect3D(reader);

            var model3DGroup = new Model3DGroup();
            _objects.Add(model3DGroup);

            if (name != string.Empty)
            {
                model3DGroup.SetName(name);
                _namedObjects.Add(name, model3DGroup);
            }

            if (transform != null)
                model3DGroup.Transform = transform;

            AddModelToParent(model3DGroup, parentIndex);

            return model3DGroup;
        }

        private GeometryModel3D DeserializeGeometryModel3D(Chunk chunk)
        {
            var geometryModel3D  = new GeometryModel3D();
            BinaryReader reader = chunk.Reader;

            string name = ReadUnicodeString(reader);

            int meshIndex         = reader.ReadInt32();
            int materialIndex     = reader.ReadInt32();
            int backMaterialIndex = reader.ReadInt32();
            int parentIndex       = reader.ReadInt32();

            Rect3D bounds = ReadRect3D(reader);

            Transform3D transform = DeserializeTransform(reader);
            if (transform != null)
                geometryModel3D.Transform = transform;



            if (name != string.Empty)
            {
                geometryModel3D.SetName(name);
                _namedObjects[name] = geometryModel3D;
            }

            if (meshIndex >= 0)
                geometryModel3D.Geometry = _meshes[meshIndex];
            
            if (materialIndex >= 0)
                geometryModel3D.Material = _materials[materialIndex];

            if (backMaterialIndex >= 0)
                geometryModel3D.BackMaterial = _materials[backMaterialIndex];


            AddModelToParent(geometryModel3D, parentIndex);

            _objects.Add(geometryModel3D);


            if (IsLogging)
            {
                Log(string.Format("     GeometryModel3D name: {0}; ParentIndex: {1}, MeshIndex: {2}; MaterialIndex: {3}; BackMaterialIndex: {4}",
                                  name, parentIndex, meshIndex, materialIndex, backMaterialIndex));
            }

            return geometryModel3D;
        }

        private void AddModelToParent(Model3D model, int parentIndex)
        {
            if (parentIndex >= 0)
            {
                if (parentIndex >= _objects.Count)
                    throw new FileFormatException("Invalid parent index " + parentIndex);

                Model3D parent = _objects[parentIndex];

                if (parent == null)
                    throw new FileFormatException("Cannot find parent object with index " + parentIndex);
                else if (!(parent is Model3DGroup))
                    throw new FileFormatException("Parent object is not Model3DGroup!");
                else
                    ((Model3DGroup)parent).Children.Add(model);
            }
        }

        private Material DeserializeMaterial(BinaryReader reader)
        {
            Chunk chunk = Chunk.ReadChunkHeader(reader);

            Material material = DeserializeMaterial(chunk);

            chunk.EndReadChunk();

            return material;
        }

        private Material DeserializeMaterial(Chunk chunk)
        {
            Material material;

            BinaryReader reader = chunk.Reader;

            if (chunk.Type == ObjectTypes.Null)
            {
                material = null;
            }
            else if (chunk.Type == ObjectTypes.DiffuseMaterial)
            {
                DiffuseMaterial diffuseMaterial = new DiffuseMaterial();
                diffuseMaterial.AmbientColor = ReadColor(reader);
                diffuseMaterial.Color = ReadColor(reader);
                diffuseMaterial.Brush = DeserializeBrush(reader);

                material = diffuseMaterial;
            }
            else if (chunk.Type == ObjectTypes.SpecularMaterial)
            {
                SpecularMaterial specularMaterial = new SpecularMaterial();
                specularMaterial.SpecularPower = reader.ReadDouble();
                specularMaterial.Color = ReadColor(reader);
                specularMaterial.Brush = DeserializeBrush(reader);

                material = specularMaterial;
            }
            else if (chunk.Type == ObjectTypes.MaterialGroup)
            {
                MaterialGroup materialGroup = new MaterialGroup();

                int childrenCount = reader.ReadInt32();

                for (int i = 0; i < childrenCount; i++)
                    materialGroup.Children.Add(DeserializeMaterial(reader));

                material = materialGroup;
            }
            else
            {
#if DEBUG
                throw new Exception("Unsupported material type!");
#else
                // Skip unknown material
                chunk.EndReadChunk();

                material = null;
#endif
            }

            if (IsLogging)
            {
                Log(string.Format("     {0} Index: {1}", material != null ? material.GetType().Name : "<null>", _currentMeshIndex));
            }

            return material;
        }


        private Brush DeserializeBrush(BinaryReader reader)
        {
            Chunk chunk = Chunk.ReadChunkHeader(reader);

            Brush brush = DeserializeBrush(chunk);

            chunk.EndReadChunk();

            return brush;
        }

        private ImageBrush DeserializeImageBrush(Chunk chunk)
        {
            BinaryReader reader = chunk.Reader;

            string fileName = ReadUnicodeString(reader);

            if (IsLogging)
                Log("Start creating ImageBrush from file " + fileName);


            // Check if we need to convert relative path to an absolute path
            if (!System.IO.Path.IsPathRooted(fileName) && !string.IsNullOrEmpty(SourceFileName))
                fileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(SourceFileName), fileName);

            if (ResolveTextureFileName != null)
                fileName = ResolveTextureFileName(fileName);

            BitmapImage bitmapImage;
            if (System.IO.File.Exists(fileName))
            {
                Log("Creating BitmapImage from " + fileName);
                bitmapImage = new BitmapImage(new Uri(fileName, UriKind.RelativeOrAbsolute));
            }
            else
            {
                Log("Cannot find texture file " + fileName);
                bitmapImage = null;
            }

            var imageBrush = new ImageBrush(bitmapImage);
            imageBrush.Opacity           = reader.ReadDouble();
            imageBrush.TileMode          = (TileMode)reader.ReadInt32();
            imageBrush.Stretch           = (Stretch)reader.ReadInt32();
            imageBrush.ViewportUnits     = (BrushMappingMode)reader.ReadInt32();
            imageBrush.Viewport          = new Rect(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
            imageBrush.ViewboxUnits      = (BrushMappingMode)reader.ReadInt32();
            imageBrush.Viewbox           = new Rect(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
            imageBrush.Transform         = new MatrixTransform(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
            imageBrush.RelativeTransform = new MatrixTransform(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());

            return imageBrush;
        }

        private Brush DeserializeBrush(Chunk chunk)
        {
            Brush brush;

            BinaryReader reader = chunk.Reader;

            if (chunk.Type == ObjectTypes.SolidColorBrush)
            {
                SolidColorBrush solidColorBrush = new SolidColorBrush();
                solidColorBrush.Color = ReadColor(reader);
                solidColorBrush.Opacity = reader.ReadDouble();

                brush = solidColorBrush;
            }
            else if (chunk.Type == ObjectTypes.ImageBrush)
            {
                brush = DeserializeImageBrush(chunk);
            }
            else if (chunk.Type == ObjectTypes.Null)
            {
                brush = null;
            }
            else
            {
                brush = null;
            }

            return brush;
        }

        private Transform3D DeserializeTransform(BinaryReader reader)
        {
            Chunk chunk = Chunk.ReadChunkHeader(reader);

            Transform3D transform = DeserializeTransform(chunk);

            chunk.EndReadChunk();

            return transform;
        }

        private Transform3D DeserializeTransform(Chunk chunk)
        {
            Transform3D transform;
            var reader = chunk.Reader;

            if (chunk.Type == ObjectTypes.Null)
            {
                transform = null;
            }
            else if (chunk.Type == ObjectTypes.IdentityMatrix3D)
            {
                transform = new MatrixTransform3D(Matrix3D.Identity);
            }
            else if (chunk.Type == ObjectTypes.MatrixTransform3D)
            {
                Matrix3D matrix = ReadMatrix(reader);
                transform = new MatrixTransform3D(matrix);
            }
            else if (chunk.Type == ObjectTypes.TranslateTransform3D)
            {
                transform = new TranslateTransform3D(ReadOneValue(reader), ReadOneValue(reader), ReadOneValue(reader));
            }
            else if (chunk.Type == ObjectTypes.ScaleTransform3D)
            {
                transform = new ScaleTransform3D(ReadOneValue(reader), ReadOneValue(reader), ReadOneValue(reader));
            }
            else if (chunk.Type == ObjectTypes.AxisAngleRotateTransform3D)
            {
                var axisAngleRotation3D = new AxisAngleRotation3D(ReadVector3D(reader), ReadOneValue(reader));
                transform = new RotateTransform3D(axisAngleRotation3D, ReadPoint3D(reader));
            }
            else if (chunk.Type == ObjectTypes.QuaternionRotateTransform3D)
            {
                var quaternionRotation3D = new QuaternionRotation3D(new Quaternion(ReadOneValue(reader), ReadOneValue(reader), ReadOneValue(reader), ReadOneValue(reader)));
                transform = new RotateTransform3D(quaternionRotation3D, ReadPoint3D(reader));
            }
            else if (chunk.Type == ObjectTypes.Transform3DGroup)
            {
                int childrenCount = reader.ReadInt32();

                var transform3DGroup = new Transform3DGroup();

                for (int i = 0; i < childrenCount; i++)
                {
                    var childTransform = DeserializeTransform(reader);
                    transform3DGroup.Children.Add(childTransform);
                }

                transform = transform3DGroup;
            }
            else 
            {
#if DEBUG
                throw new NotSupportedException("Not supported transform type");

#else
                transform = null;

                // Skip unknown transform
                chunk.EndReadChunk();
#endif
            }

            return transform;
        }

        private MeshGeometry3D DeserializeMeshGeometry3D(Chunk chunk)
        {
            Point3DCollection  positions       = null;
            Vector3DCollection normals         = null;
            PointCollection    textureCoords   = null;
            Int32Collection    indices         = null;
            Int32Collection    polygonIndices  = null;
            Int32Collection    edgeLineIndices = null;

            BinaryReader reader = chunk.Reader;

            long chunkEndPosition = reader.BaseStream.Position + chunk.ChunkLength;
            int dataUnitSize = DataPrecision == DataPrecisionType.Double ? SizeOfDouble : SizeOfFloat;
            int count;

            // Read bounds
            Rect3D bounds = ReadRect3D(reader);

            while (reader.BaseStream.Position < chunkEndPosition)
            {
                var dataChunk = Chunk.ReadChunkHeader(reader);

                switch (dataChunk.Type)
                {
                    case ObjectTypes.MeshPositions:
                        count = dataChunk.ChunkLength / (3 * dataUnitSize);
                        positions = new Point3DCollection(count);

                        if (DataPrecision == DataPrecisionType.Double)
                        {
                            for (int i = 0; i < count; i++)
                                positions.Add(new Point3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble()));
                        }
                        else //if (DataPrecision == DataPrecisionType.Float)
                        {
                            for (int i = 0; i < count; i++)
                                positions.Add(new Point3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                        }
                        break;
                    
                    case ObjectTypes.MeshNormals:
                        count = dataChunk.ChunkLength / (3 * dataUnitSize);

                        normals = new Vector3DCollection(count);

                        if (DataPrecision == DataPrecisionType.Double)
                        {
                            for (int i = 0; i < count; i++)
                                normals.Add(new Vector3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble()));
                        }
                        else //if (DataPrecision == DataPrecisionType.Float)
                        {
                            for (int i = 0; i < count; i++)
                                normals.Add(new Vector3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
                        }

                        break;
                    
                    case ObjectTypes.MeshTextureCoordinates:
                        count = dataChunk.ChunkLength / (2 * dataUnitSize);

                        textureCoords = new PointCollection(count);

                        if (DataPrecision == DataPrecisionType.Double)
                        {
                            for (int i = 0; i < count; i++)
                                textureCoords.Add(new Point(reader.ReadDouble(), reader.ReadDouble()));
                        }
                        else //if (DataPrecision == DataPrecisionType.Float)
                        {
                            for (int i = 0; i < count; i++)
                                textureCoords.Add(new Point(reader.ReadSingle(), reader.ReadSingle()));
                        }

                        break;
                    
                    case ObjectTypes.MeshTriangleIndices_16bit:
                        count = dataChunk.ChunkLength / SizeOfInt16;

                        indices = new Int32Collection(count);
                        for (int i = 0; i < count; i++)
                            indices.Add(reader.ReadUInt16());

                        break;
                    
                    case ObjectTypes.MeshTriangleIndices_32bit:
                        count = dataChunk.ChunkLength / SizeOfInt32;

                        indices = new Int32Collection(count);
                        for (int i = 0; i < count; i++)
                            indices.Add(reader.ReadInt32());

                        break;

                    case ObjectTypes.MeshPolygonIndices:
                        count = dataChunk.ChunkLength / SizeOfInt32;

                        polygonIndices = new Int32Collection(count);
                        for (int i = 0; i < count; i++)
                            polygonIndices.Add(reader.ReadInt32());

                        break;
                    
                    case ObjectTypes.MeshEdgeLineIndices:
                        count = dataChunk.ChunkLength / SizeOfInt32;

                        edgeLineIndices = new Int32Collection(count);
                        for (int i = 0; i < count; i++)
                            edgeLineIndices.Add(reader.ReadInt32());

                        break;
                }

                dataChunk.EndReadChunk();
            }


            MeshGeometry3D mesh = new MeshGeometry3D();

            if (positions != null)
                mesh.Positions = positions;

            if (normals != null)
                mesh.Normals = normals;

            if (textureCoords != null)
                mesh.TextureCoordinates = textureCoords;

            if (indices != null)
                mesh.TriangleIndices = indices;
            
            if (polygonIndices != null)
                mesh.SetPolygonIndices(polygonIndices);
            
            if (edgeLineIndices != null)
                mesh.SetValue(MeshUtils.EdgeLineIndicesProperty, edgeLineIndices);


            if (IsLogging)
            {
                Log(string.Format("     MeshGeometry3D Index: {0}; PositionsCount: {1}; TriangleIndicesCount: {2}",
                    _currentMeshIndex, positions != null ? positions.Count : 0, indices != null ? indices.Count : 0));
            }


            return mesh;
        }


        private object DeserializeCamera(Chunk chunk)
        {
            object retCamera = null;

            BinaryReader reader = chunk.Reader;

            var xaml = ReadAsciiString(reader);

            if (!string.IsNullOrEmpty(xaml))
            {
                try
                {
                    retCamera = XamlReader.Parse(xaml);
                }
                catch (Exception ex)
                {
                    Log("Error parsing camera xaml: " + ex.Message);
                }
            }

            return retCamera;
        }


        private BitmapImage DeserializeBitmap(Chunk chunk)
        {
            if (chunk.Type != ObjectTypes.Bitmap)
                return null;

            BitmapImage bitmap = null;
            BinaryReader reader = chunk.Reader;

            int width        = reader.ReadInt32();
            int height       = reader.ReadInt32();
            int bitsPerPixel = reader.ReadInt32();

            int bitmapLength = reader.ReadInt32();

            byte[] bitmapArray = reader.ReadBytes(bitmapLength);

            using (MemoryStream ms = new MemoryStream(bitmapArray))
            {
                bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DecodePixelWidth = width;
                bitmap.DecodePixelHeight = height;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
            }

            bitmap.Freeze(); // Freeze the bitmap so it can be read in BG thread

            return bitmap;
        }
        #endregion

        #region Serialization
        private void SerializeModel3D(Model3D model, int parentObjectIndex, BinaryWriter writer)
        {
            if (model is Model3DGroup)
                SerializeModel3DGroup((Model3DGroup)model, parentObjectIndex, writer);
            else if (model is GeometryModel3D)
                SerializeGeometryModel3D((GeometryModel3D)model, parentObjectIndex, writer);
            else
                Chunk.BeginWriteChunk(ObjectTypes.Unknown, 0, writer); // unsupported model (for example light) is written as null
        }

        private void SerializeModel3DGroup(Model3DGroup modelGroup, int parentObjectIndex, BinaryWriter writer)
        {
            string name = GetObjectName(modelGroup);

            int chunkLength = GetSizeOfUnicodeString(name)
                              + GetSizeOfTransform(modelGroup.Transform)
                              + 2 * SizeOfInt32;

            if (DataPrecision == DataPrecisionType.Double)
                chunkLength += SizeOfRect3D; // Bounds
            else
                chunkLength += SizeOfFloatRect3D; // Bounds


            _currentObjectIndex++;
            var thisObjectIndex = _currentObjectIndex;

            Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.Model3DGroup, chunkLength, writer);

            WriteUnicodeString(name, writer);

            SerializeTransform3D(modelGroup.Transform, writer);

            writer.Write(modelGroup.Children.Count);
            writer.Write(parentObjectIndex);

            WriteRect3D(modelGroup.Bounds, writer);

            chunk.EndWriteChunk();

            if (IsLogging)
                Log(string.Format("     Model3DGroup name: {0}, children count: {1}, Current object index: {2}, Parent object index: {3}", name, modelGroup.Children.Count, _currentObjectIndex, parentObjectIndex));

            foreach (var oneChild in modelGroup.Children)
                SerializeModel3D(oneChild, thisObjectIndex, writer);

            Log("     end of Model3DGroup");
        }

        private void SerializeGeometryModel3D(GeometryModel3D geometryModel3D, int parentObjectIndex, BinaryWriter writer)
        {
            string name = GetObjectName(geometryModel3D);

            int chunkLength = GetSizeOfUnicodeString(name)
                              + 4 * SizeOfInt32    // Parent index + mesh index + Material index + BackMaterial index
                              + GetSizeOfTransform(geometryModel3D.Transform);

            if (DataPrecision == DataPrecisionType.Double)
                chunkLength += SizeOfRect3D; // Bounds
            else
                chunkLength += SizeOfFloatRect3D; // Bounds


            _currentObjectIndex++;

            int meshIndex;
            var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;
            if (meshGeometry3D != null)
                meshIndex = _meshIndexes[meshGeometry3D];
            else
                meshIndex = -1; 

            int materialIndex;
            var material = geometryModel3D.Material;
            if (material != null)
                materialIndex = _materialIndexes[material];
            else
                materialIndex = -1;
            
            int backMaterialIndex;
            material = geometryModel3D.BackMaterial;
            if (material != null)
                backMaterialIndex = _materialIndexes[material];
            else
                backMaterialIndex = -1;


            Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.GeometryModel3D, chunkLength, writer);

            WriteUnicodeString(name, writer);
            
            writer.Write(meshIndex);
            writer.Write(materialIndex);
            writer.Write(backMaterialIndex);

            writer.Write(parentObjectIndex);

            WriteRect3D(geometryModel3D.Bounds, writer);

            SerializeTransform3D(geometryModel3D.Transform, writer);

            chunk.EndWriteChunk();


            if (IsLogging)
            {
                Log(string.Format("     GeometryModel3D name: {0}, Current object index: {1}, ParentIndex: {2}; MeshIndex: {3}; MaterialIndex: {4}; BackMaterialIndex: {5}",
                    name, _currentObjectIndex, parentObjectIndex, meshGeometry3D, materialIndex, backMaterialIndex));
            }
        }

        private void SerializeMaterial(Material material, BinaryWriter writer)
        {
            if (material == null)
            {
                Chunk.BeginWriteChunk(ObjectTypes.Null, 0, writer);
            }
            else if (material is DiffuseMaterial)
            {
                DiffuseMaterial diffuseMaterial = (DiffuseMaterial)material;

                Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.DiffuseMaterial, SizeOfColor + SizeOfColor + Chunk.SizeOfChunkHeader + GetSizeOfBrush(diffuseMaterial.Brush), writer);

                WriteColor(diffuseMaterial.AmbientColor, writer);
                WriteColor(diffuseMaterial.Color, writer);
                SerializeBrush(diffuseMaterial.Brush, writer);

                chunk.EndWriteChunk();
            }
            else if (material is SpecularMaterial)
            {
                SpecularMaterial specularMaterial = (SpecularMaterial)material;

                Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.SpecularMaterial, SizeOfDouble + SizeOfColor + Chunk.SizeOfChunkHeader + GetSizeOfBrush(specularMaterial.Brush), writer);

                writer.Write(specularMaterial.SpecularPower);
                WriteColor(specularMaterial.Color, writer);

                SerializeBrush(specularMaterial.Brush, writer);

                chunk.EndWriteChunk();
            }
            else if (material is MaterialGroup)
            {
                MaterialGroup materialGroup = (MaterialGroup)material;

                Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.MaterialGroup, GetSizeOfMaterial(materialGroup), writer);
                writer.Write(materialGroup.Children.Count);

                foreach (var oneChild in materialGroup.Children)
                    SerializeMaterial(oneChild, writer);

                chunk.EndWriteChunk();
            }
            else
            {
                throw new NotSupportedException("Unsupported Material!");
            }
        }


        private void SerializeTransform3D(Transform3D transform, BinaryWriter writer)
        {
            if (transform is TranslateTransform3D)
            {
                SerializeTranslateTransform3D((TranslateTransform3D)transform, writer);
            }
            else if (transform is ScaleTransform3D)
            {
                SerializeScaleTransform3D((ScaleTransform3D)transform, writer);
            }
            else if (transform is RotateTransform3D)
            {
                SerializeRotateTransform3D((RotateTransform3D)transform, writer);
            }
            else if (transform is MatrixTransform3D)
            {
                SerializeMatrix3D(((MatrixTransform3D)transform).Matrix, writer);
            }
            else if (transform is Transform3DGroup)
            {
                SerializeTransform3DGroup((Transform3DGroup)transform, writer);
            }
            else 
            {
                // transform == null or unknown transform type
                Chunk.BeginWriteChunk(ObjectTypes.Null, 0, writer);
            }
        }

        private void SerializeMatrix3D(Matrix3D matrix, BinaryWriter writer)
        {
            Chunk chunk;

            if (matrix.IsIdentity)
            {
                chunk = Chunk.BeginWriteChunk(ObjectTypes.IdentityMatrix3D, 0, writer);
            }
            else
            {
                int chunkSize;
                if (DataPrecision == DataPrecisionType.Double)
                    chunkSize = SizeOfMatrix3D;
                else if (DataPrecision == DataPrecisionType.Float)
                    chunkSize = SizeOfFloatMatrix3D;
                else
                    chunkSize = 0;

                chunk = Chunk.BeginWriteChunk(ObjectTypes.MatrixTransform3D, chunkSize, writer);

                WriteMatrix(matrix, writer);
            }

            chunk.EndWriteChunk();
        }
        
        private void SerializeTranslateTransform3D(TranslateTransform3D translateTransform3D, BinaryWriter writer)
        {
            int chunkSize = GetSizeOfData(3);
            Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.TranslateTransform3D, chunkSize, writer);

            Write(translateTransform3D.OffsetX, translateTransform3D.OffsetY, translateTransform3D.OffsetZ, writer);

            chunk.EndWriteChunk();
        }

        private void SerializeScaleTransform3D(ScaleTransform3D scaleTransform3D, BinaryWriter writer)
        {
            int chunkSize = GetSizeOfData(3);
            Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.ScaleTransform3D, chunkSize, writer);

            Write(scaleTransform3D.ScaleX, scaleTransform3D.ScaleY, scaleTransform3D.ScaleZ, writer);

            chunk.EndWriteChunk();
        }

        // write AxisAngleRotateTransform3D: axis_vector3D + angle + rotation_center
        // write QuaternionRotateTransform3D: quaternion (4 values) + rotation_center
        private void SerializeRotateTransform3D(RotateTransform3D rotateTransform3D, BinaryWriter writer)
        {
            int chunkSize = GetSizeOfData(7);
            var axisAngleRotation3D = rotateTransform3D.Rotation as AxisAngleRotation3D;

            Chunk chunk;

            if (axisAngleRotation3D != null)
            {
                chunk = Chunk.BeginWriteChunk(ObjectTypes.AxisAngleRotateTransform3D, chunkSize, writer);

                Write(axisAngleRotation3D.Axis,  writer);
                Write(axisAngleRotation3D.Angle, writer);
                Write(rotateTransform3D.CenterX, rotateTransform3D.CenterY, rotateTransform3D.CenterZ, writer);
            }
            else
            {
                var quaternionRotation3D = rotateTransform3D.Rotation as QuaternionRotation3D;

                if (quaternionRotation3D != null)
                {
                    chunk = Chunk.BeginWriteChunk(ObjectTypes.QuaternionRotateTransform3D, chunkSize, writer);

                    Write(quaternionRotation3D.Quaternion.X, quaternionRotation3D.Quaternion.Y, quaternionRotation3D.Quaternion.Z, quaternionRotation3D.Quaternion.W, writer);
                    Write(rotateTransform3D.CenterX, rotateTransform3D.CenterY, rotateTransform3D.CenterZ, writer);
                }
                else
                {
                    // Unknown rotation type
                    chunk = Chunk.BeginWriteChunk(ObjectTypes.Null, 0, writer);
                }
            }

            chunk.EndWriteChunk();
        }

        private void SerializeTransform3DGroup(Transform3DGroup transform3DGroup, BinaryWriter writer)
        {
            int chunkSize = GetSizeOfTransform(transform3DGroup) - Chunk.SizeOfChunkHeader; // subtract one header as header for this chunk should not be included

            Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.Transform3DGroup, chunkSize, writer);

            writer.Write(transform3DGroup.Children.Count);

            foreach (var transform3D in transform3DGroup.Children)
                SerializeTransform3D(transform3D, writer);

            chunk.EndWriteChunk();
        }

        private void SerializeMeshGeometry3D(MeshGeometry3D mesh, BinaryWriter writer)
        {
            int chunkLength  = GetSizeOfMeshGeometry3D(mesh);
            int dataUnitSize = DataPrecision == DataPrecisionType.Double ? SizeOfDouble : SizeOfFloat;

            Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.MeshGeometry3D, chunkLength, writer);

            // Write bounds
            WriteRect3D(mesh.Bounds, writer);
            
            // Positions
            var positions = mesh.Positions;
            var dataChunk = Chunk.BeginWriteChunk(ObjectTypes.MeshPositions, positions.Count * 3 * dataUnitSize, writer);

            for (int i = 0; i < positions.Count; i++)
                Write(positions[i].X, positions[i].Y, positions[i].Z, writer);

            dataChunk.EndWriteChunk();


            // Normals
            if (SaveNormals)
            {
                var normals = mesh.Normals;
                if (normals != null && normals.Count > 0)
                {
                    dataChunk = Chunk.BeginWriteChunk(ObjectTypes.MeshNormals, normals.Count * 3 * dataUnitSize, writer);

                    for (int i = 0; i < normals.Count; i++)
                        Write(normals[i].X, normals[i].Y, normals[i].Z, writer);

                    dataChunk.EndWriteChunk();
                }
            }


            // TextureCoordinates
            if (SaveTextureCoordinates)
            {
                var textureCoordinates = mesh.TextureCoordinates;
                if (textureCoordinates != null && textureCoordinates.Count > 0)
                {
                    dataChunk = Chunk.BeginWriteChunk(ObjectTypes.MeshTextureCoordinates, textureCoordinates.Count * 2 * dataUnitSize, writer);

                    for (int i = 0; i < textureCoordinates.Count; i++)
                        Write(textureCoordinates[i].X, textureCoordinates[i].Y, writer);

                    dataChunk.EndWriteChunk();
                }
            }


            // TriangleIndices
            var triangleIndices = mesh.TriangleIndices;
            if (triangleIndices != null && triangleIndices.Count > 0)
            {
                if (positions.Count < UInt16.MaxValue) // Check if we can write TriangleIndices as 16 bit UInt16 (ushort)
                {
                    dataChunk = Chunk.BeginWriteChunk(ObjectTypes.MeshTriangleIndices_16bit, triangleIndices.Count * SizeOfInt16, writer);

                    for (int i = 0; i < triangleIndices.Count; i++)
                        writer.Write((UInt16)triangleIndices[i]);

                    dataChunk.EndWriteChunk();
                }
                else
                {
                    dataChunk = Chunk.BeginWriteChunk(ObjectTypes.MeshTriangleIndices_32bit, triangleIndices.Count * SizeOfInt32, writer);

                    for (int i = 0; i < triangleIndices.Count; i++)
                        writer.Write(triangleIndices[i]);

                    dataChunk.EndWriteChunk();
                }
            }


            // PolygonIndices
            var polygonIndices = mesh.GetPolygonIndices();
            if (polygonIndices != null && polygonIndices.Count > 0)
            {
                dataChunk = Chunk.BeginWriteChunk(ObjectTypes.MeshPolygonIndices, polygonIndices.Count * SizeOfInt32, writer);

                for (int i = 0; i < polygonIndices.Count; i++)
                    writer.Write(polygonIndices[i]);

                dataChunk.EndWriteChunk();
            }


            // EdgeLines
            var edgeLines = mesh.GetValue(MeshUtils.EdgeLineIndicesProperty) as Int32Collection;
            if (edgeLines != null && edgeLines.Count > 0)
            {
                dataChunk = Chunk.BeginWriteChunk(ObjectTypes.MeshPolygonIndices, edgeLines.Count * SizeOfInt32, writer);

                for (int i = 0; i < edgeLines.Count; i++)
                    writer.Write(edgeLines[i]);

                dataChunk.EndWriteChunk();
            }


            chunk.EndWriteChunk();
        }

        private void SerializeBrush(Brush brush, BinaryWriter writer)
        {
            if (brush == null)
            {
                Chunk.BeginWriteChunk(ObjectTypes.Null, 0, writer);
            }
            else if (brush is SolidColorBrush)
            {
                SolidColorBrush solidColorBrush = (SolidColorBrush)brush;

                Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.SolidColorBrush, SizeOfColor + SizeOfDouble, writer);

                WriteColor(solidColorBrush.Color, writer);
                writer.Write(solidColorBrush.Opacity);

                chunk.EndWriteChunk();
            }
            else if (brush is ImageBrush)
            {
                SerializeImageBrush((ImageBrush) brush, writer);
            }
            else
            {
                throw new NotSupportedException("Not supported brush:" + brush.GetType().Name);
            }
        }

        private void SerializeImageBrush(ImageBrush imageBrush, BinaryWriter writer)
        {
            var bitmapImage = imageBrush.ImageSource as BitmapImage;
            
            if (bitmapImage == null)
                throw new NotSupportedException("Only ImageBrush with BitmapImage can be exported!");


            var fileName = GetBitmapImageFileName(bitmapImage);

            if (fileName == null)
                throw new Exception("Cannot get file name from the BitmapImage. Only BitmapImage that are created from files can be exported!");



            int chunkLength = GetSizeOfImageBrush(imageBrush);
            Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.ImageBrush, chunkLength, writer);

            // Write file name
            WriteUnicodeString(fileName, writer);

            // Write ImageBrush properties
            writer.Write(imageBrush.Opacity);
            writer.Write((int)imageBrush.TileMode);
            writer.Write((int)imageBrush.Stretch);
            writer.Write((int)imageBrush.ViewportUnits);
            writer.Write(imageBrush.Viewport.X);
            writer.Write(imageBrush.Viewport.Y);
            writer.Write(imageBrush.Viewport.Width);
            writer.Write(imageBrush.Viewport.Height);
            writer.Write((int)imageBrush.ViewboxUnits);
            writer.Write(imageBrush.Viewbox.X);
            writer.Write(imageBrush.Viewbox.Y);
            writer.Write(imageBrush.Viewbox.Width);
            writer.Write(imageBrush.Viewbox.Height);
            writer.Write(imageBrush.Transform.Value.M11);
            writer.Write(imageBrush.Transform.Value.M12);
            writer.Write(imageBrush.Transform.Value.M21);
            writer.Write(imageBrush.Transform.Value.M22);
            writer.Write(imageBrush.Transform.Value.OffsetX);
            writer.Write(imageBrush.Transform.Value.OffsetY);
            writer.Write(imageBrush.RelativeTransform.Value.M11);
            writer.Write(imageBrush.RelativeTransform.Value.M12);
            writer.Write(imageBrush.RelativeTransform.Value.M21);
            writer.Write(imageBrush.RelativeTransform.Value.M22);
            writer.Write(imageBrush.RelativeTransform.Value.OffsetX);
            writer.Write(imageBrush.RelativeTransform.Value.OffsetY);

            chunk.EndWriteChunk();
        }

        // GetRelativePath and AppendDirectorySeparatorChar are from: https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
        private static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (string.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(AppendDirectorySeparatorChar(fromPath));
            Uri toUri   = new Uri(AppendDirectorySeparatorChar(toPath));

            if (fromUri.Scheme != toUri.Scheme)
                return toPath;

            Uri    relativeUri  = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (string.Equals(toUri.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            return relativePath;
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            // Append a slash only if the path is a directory and does not have a slash.
            if (!Path.HasExtension(path) &&
                !path.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        private string GetBitmapImageFileName(BitmapImage bitmapImage)
        {
            string fileName = null;

            if (bitmapImage.UriSource != null)
            {
                if (bitmapImage.UriSource.IsAbsoluteUri) // NOTE: If the Uri is relative (IsAbsoluteUri is false), then LocalPath getter throws an exception
                {
                    try
                    {
                        if (bitmapImage.UriSource.IsFile) // IsFile is true for local files (not for files in resources)
                            fileName = bitmapImage.UriSource.LocalPath;
                    }
                    catch (Exception ex)
                    {
                        Log("Error reading LocalPath: " + ex.Message);
                    }
                }
                else
                {
                    // We have a relative uri and cannot get LocalPath property (an exception is thrown)
                    // Therefore we use OriginalString property                     
                    try
                    {
                        fileName = bitmapImage.UriSource.OriginalString;
                    }
                    catch (Exception ex)
                    {
                        Log("Error reading OriginalString: " + ex.Message);
                    }
                }
            }
            else
            {
                // Try to get file name from FileStream (for example this is used in ReaderObj from Ab3d.PowerToys - to prevent locking the image file that happens when UriSource is set)
                var fileStream = bitmapImage.StreamSource as FileStream;
                if (fileStream != null && !string.IsNullOrEmpty(fileStream.Name))
                {
                    fileName = fileStream.Name;

                    try
                    {
                        if (!System.IO.File.Exists(fileName))
                            fileName = null;
                    }
                    catch (Exception ex)
                    {
                        Log("Error checking if TextureResourceName exist: " + ex.Message);
                    }
                }
            }

            // If SourceFileName is set, then we save the texture path as relative path to the source file
            if (fileName != null && !string.IsNullOrEmpty(SourceFileName))
            {
                // In .Net CORE we could use Path.GetRelativePath
                // But here we use a solution from https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path
                fileName = GetRelativePath(System.IO.Path.GetDirectoryName(SourceFileName), fileName);
            }

            return fileName;
        }

        private void SerializeCamera(object camera, BinaryWriter writer)
        {
            if (camera == null)
                return;

            string xaml;

            try
            {
                xaml = XamlWriter.Save(camera);
            }
            catch (Exception ex)
            {
                xaml = null;
                Log("Error converting camera to xaml text: " + ex.Message);
            }

            if (xaml != null)
            {
                Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.CameraXaml, GetSizeOfAsciiString(xaml), writer);

                WriteAsciiString(xaml, writer);

                chunk.EndWriteChunk();
            }
        }

        private byte[] GetBitmapBytes(BitmapSource bitmap)
        {
            byte[] bitmapBytes;

            if (bitmap == null || bitmap.PixelWidth <= 0 || bitmap.PixelHeight <= 0)
                return null;

            using (MemoryStream ms = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();

                BitmapFrame bitmapImage = BitmapFrame.Create(bitmap);
                encoder.Frames.Add(bitmapImage);
                encoder.Save(ms);

                // UH: It is not possible to write MemoryStream to BinaryWriter. Therefore we get byte array from the MemoryStream.
                bitmapBytes = ms.GetBuffer();
            }

            return bitmapBytes;
        }

        private void SerializeBitmap(byte[] bitmapBytes, int width, int height, int bitsPerPixel, BinaryWriter writer)
        {
            if (bitmapBytes == null)
            {
                Chunk.BeginWriteChunk(ObjectTypes.Null, 0, writer);
                return;
            }

            int bitmapLength = bitmapBytes.Length;

            int chunkLength = 4 * SizeOfInt32 + bitmapLength;

            Chunk chunk = Chunk.BeginWriteChunk(ObjectTypes.Bitmap, chunkLength, writer);


            writer.Write(width);
            writer.Write(height);
            writer.Write(bitsPerPixel);

            writer.Write(bitmapLength);

            writer.Write(bitmapBytes, 0, bitmapLength);

            chunk.EndWriteChunk();
        }

        private string GetObjectName(Model3D model)
        {
            string name = model.GetName();

            if (name != null)
                return name;

            // check _objectNames dictionary
            if (_objectNames == null)
                return "";

            if (!_objectNames.TryGetValue(model, out name))
                name = "";

            return name;
        }
        #endregion

        #region Simple Read... and Write... methods

        private double ReadOneValue(BinaryReader reader)
        {
            if (DataPrecision == DataPrecisionType.Double)
                return reader.ReadDouble();

            return reader.ReadSingle();
        }
        
        private Vector3D ReadVector3D(BinaryReader reader)
        {
            if (DataPrecision == DataPrecisionType.Double)
                return new Vector3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());

            return new Vector3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
        
        private Point3D ReadPoint3D(BinaryReader reader)
        {
            if (DataPrecision == DataPrecisionType.Double)
                return new Point3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());

            return new Point3D(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
        
        private void Read(BinaryReader reader, out double a, out double b)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                a = reader.ReadDouble();
                b = reader.ReadDouble();
            }
            else //if (DataPrecision == DataPrecisionType.Float)
            {
                a = reader.ReadSingle();
                b = reader.ReadSingle();
            }
        }
        
        private void Read(BinaryReader reader, out double a, out double b, out double c)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                a = reader.ReadDouble();
                b = reader.ReadDouble();
                c = reader.ReadDouble();
            }
            else //if (DataPrecision == DataPrecisionType.Float)
            {
                a = reader.ReadSingle();
                b = reader.ReadSingle();
                c = reader.ReadSingle();
            }
        }
        
        private void Read(BinaryReader reader, out double a, out double b, out double c, out double d)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                a = reader.ReadDouble();
                b = reader.ReadDouble();
                c = reader.ReadDouble();
                d = reader.ReadDouble();
            }
            else //if (DataPrecision == DataPrecisionType.Float)
            {
                a = reader.ReadSingle();
                b = reader.ReadSingle();
                c = reader.ReadSingle();
                d = reader.ReadSingle();
            }
        }

        private void Write(double a, BinaryWriter writer)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                writer.Write(a);
            }
            else //if (DataPrecision == DataPrecisionType.Float)
            {
                writer.Write((float)a);
            }
        }
        
        private void Write(double a, double b, BinaryWriter writer)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                writer.Write(a);
                writer.Write(b);
            }
            else //if (DataPrecision == DataPrecisionType.Float)
            {
                writer.Write((float)a);
                writer.Write((float)b);
            }
        }

        private void Write(double a, double b, double c, BinaryWriter writer)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                writer.Write(a);
                writer.Write(b);
                writer.Write(c);
            }
            else //if (DataPrecision == DataPrecisionType.Float)
            {
                writer.Write((float)a);
                writer.Write((float)b);
                writer.Write((float)c);
            }
        }
        
        private void Write(double a, double b, double c, double d, BinaryWriter writer)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                writer.Write(a);
                writer.Write(b);
                writer.Write(c);
                writer.Write(d);
            }
            else //if (DataPrecision == DataPrecisionType.Float)
            {
                writer.Write((float)a);
                writer.Write((float)b);
                writer.Write((float)c);
                writer.Write((float)d);
            }
        }

        private void Write(Vector3D vector3D, BinaryWriter writer)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                writer.Write(vector3D.X);
                writer.Write(vector3D.Y);
                writer.Write(vector3D.Z);
            }
            else //if (DataPrecision == DataPrecisionType.Float)
            {
                writer.Write((float)vector3D.X);
                writer.Write((float)vector3D.Y);
                writer.Write((float)vector3D.Z);
            }
        }
        
        private void Write(Point3D point3D, BinaryWriter writer)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                writer.Write(point3D.X);
                writer.Write(point3D.Y);
                writer.Write(point3D.Z);
            }
            else //if (DataPrecision == DataPrecisionType.Float)
            {
                writer.Write((float)point3D.X);
                writer.Write((float)point3D.Y);
                writer.Write((float)point3D.Z);
            }
        }
        
        private void Write(ref Vector3D vector3D, BinaryWriter writer)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                writer.Write(vector3D.X);
                writer.Write(vector3D.Y);
                writer.Write(vector3D.Z);
            }
            else //if (DataPrecision == DataPrecisionType.Float)
            {
                writer.Write((float)vector3D.X);
                writer.Write((float)vector3D.Y);
                writer.Write((float)vector3D.Z);
            }
        }
        
        private void Write(ref Point3D point3D, BinaryWriter writer)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                writer.Write(point3D.X);
                writer.Write(point3D.Y);
                writer.Write(point3D.Z);
            }
            else //if (DataPrecision == DataPrecisionType.Float)
            {
                writer.Write((float)point3D.X);
                writer.Write((float)point3D.Y);
                writer.Write((float)point3D.Z);
            }
        }

        private Color ReadColor(BinaryReader reader)
        {
            return Color.FromArgb(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        }

        private void WriteColor(Color color, BinaryWriter writer)
        {
            writer.Write((byte)color.A);
            writer.Write((byte)color.R);
            writer.Write((byte)color.G);
            writer.Write((byte)color.B);
        }
        
        private Matrix3D ReadMatrix(BinaryReader reader)
        {
            Matrix3D matrix;

            if (DataPrecision == DataPrecisionType.Double)
            {
                matrix = new Matrix3D(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(),
                                      reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(),
                                      reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(),
                                      reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
            }
            else if (DataPrecision == DataPrecisionType.Float)
            {
                matrix = new Matrix3D((double)reader.ReadSingle(), (double)reader.ReadSingle(), (double)reader.ReadSingle(), (double)reader.ReadSingle(),
                                      (double)reader.ReadSingle(), (double)reader.ReadSingle(), (double)reader.ReadSingle(), (double)reader.ReadSingle(),
                                      (double)reader.ReadSingle(), (double)reader.ReadSingle(), (double)reader.ReadSingle(), (double)reader.ReadSingle(),
                                      (double)reader.ReadSingle(), (double)reader.ReadSingle(), (double)reader.ReadSingle(), (double)reader.ReadSingle());
            }
            else
            {
                // Unsupported data precision
                matrix = Matrix3D.Identity;
            }

            return matrix;
        }

        private void WriteMatrix(Matrix3D matrix, BinaryWriter writer)
        {
            if (DataPrecision == DataPrecisionType.Double)
            {
                writer.Write(matrix.M11);
                writer.Write(matrix.M12);
                writer.Write(matrix.M13);
                writer.Write(matrix.M14);

                writer.Write(matrix.M21);
                writer.Write(matrix.M22);
                writer.Write(matrix.M23);
                writer.Write(matrix.M24);

                writer.Write(matrix.M31);
                writer.Write(matrix.M32);
                writer.Write(matrix.M33);
                writer.Write(matrix.M34);

                writer.Write(matrix.OffsetX);
                writer.Write(matrix.OffsetY);
                writer.Write(matrix.OffsetZ);
                writer.Write(matrix.M44);
            }
            else if (DataPrecision == DataPrecisionType.Float)
            {
                writer.Write((float)matrix.M11);
                writer.Write((float)matrix.M12);
                writer.Write((float)matrix.M13);
                writer.Write((float)matrix.M14);

                writer.Write((float)matrix.M21);
                writer.Write((float)matrix.M22);
                writer.Write((float)matrix.M23);
                writer.Write((float)matrix.M24);

                writer.Write((float)matrix.M31);
                writer.Write((float)matrix.M32);
                writer.Write((float)matrix.M33);
                writer.Write((float)matrix.M34);

                writer.Write((float)matrix.OffsetX);
                writer.Write((float)matrix.OffsetY);
                writer.Write((float)matrix.OffsetZ);
                writer.Write((float)matrix.M44);
            }
            else
            {
                // Unsupported data precision
            }
        }

        private Rect3D ReadRect3D(BinaryReader reader)
        {
            double x,  y,  z;
            double xs, ys, zs;

            Read(reader, out x, out y, out z);
            Read(reader, out xs, out ys, out zs);

            if (xs < 0)
                return Rect3D.Empty;
            
            return new Rect3D(x, y, z, xs, ys, zs);
        }

        private void WriteRect3D(Rect3D rect, BinaryWriter writer)
        {
            Write(rect.X, rect.Y, rect.Z, writer);
            Write(rect.SizeX, rect.SizeY, rect.SizeZ, writer);
        }

        private void WriteUnicodeString(string text, BinaryWriter writer)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(text);

            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private void WriteAsciiStringWithoutHeader(string text, BinaryWriter writer)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);

            writer.Write(bytes);
        }

        private string ReadAsciiStringWithoutHeader(int textLength, BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(textLength);
            string text = Encoding.ASCII.GetString(bytes);

            return text;
        }

        private void WriteAsciiString(string text, BinaryWriter writer)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);

            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private string ReadUnicodeString(BinaryReader reader)
        {
            int len = reader.ReadInt32();
            var bytes = reader.ReadBytes(len);

            string text = Encoding.Unicode.GetString(bytes);

            return text;
        }

        private string ReadAsciiString(BinaryReader reader)
        {
            int len = reader.ReadInt32();
            var bytes = reader.ReadBytes(len);

            string text = Encoding.ASCII.GetString(bytes);

            return text;
        }
        #endregion

        #region SizeOf... methods

        private const int SizeOfInt64 = 8;
        private const int SizeOfInt32 = 4;
        private const int SizeOfInt16 = 2;
        private const int SizeOfFloat = 4;
        private const int SizeOfDouble = 8;
        private const int SizeOfColor = 4;
        private const int SizeOfMatrix3D = 16 * SizeOfDouble;
        private const int SizeOfRect3D = 6 * SizeOfDouble;
        private const int SizeOfFloatMatrix3D = 16 * SizeOfFloat;
        private const int SizeOfFloatRect3D = 6 * SizeOfFloat;

        private int GetSizeOfData(int dataValuesCount)
        {
            if (DataPrecision == DataPrecisionType.Double)
                return dataValuesCount * SizeOfDouble;

            return dataValuesCount * SizeOfFloat;
        }
        
        private int GetDataValueSize()
        {
            return DataPrecision == DataPrecisionType.Double ? SizeOfDouble : SizeOfFloat;
        }

        private int GetSizeOfBrush(Brush brush)
        {
            int chunkSize;

            if (brush == null)
            {
                chunkSize = Chunk.SizeOfChunkHeader;
            }
            else if (brush is SolidColorBrush)
            {
                chunkSize = SizeOfColor + SizeOfDouble;
            }
            else if (brush is ImageBrush)
            {
                chunkSize = GetSizeOfImageBrush((ImageBrush)brush);
            }
            else
            {
                throw new NotSupportedException("Not supported brush:" + brush.GetType().Name);
            }

            return chunkSize;
        }

        private int GetSizeOfImageBrush(ImageBrush imageBrush)
        {
            int chunkLength = 0;

            var bitmapImage = imageBrush.ImageSource as BitmapImage;

            if (bitmapImage != null)
            {
                var fileName = GetBitmapImageFileName(bitmapImage);

                if (!string.IsNullOrEmpty(fileName))
                    chunkLength = GetSizeOfUnicodeString(fileName) + 4 * SizeOfInt32 + 21 * SizeOfDouble;
            }

            return chunkLength;
        }

        private int GetSizeOfAsciiString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return SizeOfInt32;

            return Encoding.ASCII.GetByteCount(text) + SizeOfInt32; // first int is size of bytes
        }

        private int GetSizeOfUnicodeString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return SizeOfInt32;

            return Encoding.Unicode.GetByteCount(text) + SizeOfInt32; // first int is size of bytes
        }

        private int GetSizeOfMaterial(Material material)
        {
            int length;

            if (material == null)
            {
                length = 0;
            }
            else if (material is DiffuseMaterial)
            {
                DiffuseMaterial diffuseMaterial = (DiffuseMaterial)material;

                length = SizeOfColor + SizeOfColor + Chunk.SizeOfChunkHeader + GetSizeOfBrush(diffuseMaterial.Brush);
            }
            else if (material is SpecularMaterial)
            {
                SpecularMaterial specularMaterial = (SpecularMaterial)material;

                length = SizeOfDouble + SizeOfColor + Chunk.SizeOfChunkHeader + GetSizeOfBrush(specularMaterial.Brush);
            }
            else if (material is MaterialGroup)
            {
                MaterialGroup materialGroup = (MaterialGroup)material;

                length = SizeOfInt32;

                foreach (var oneChild in materialGroup.Children)
                    length += Chunk.SizeOfChunkHeader + GetSizeOfMaterial(oneChild);
            }
            else
            {
                length = 0;
            }

            return length;
        }

        private int GetSizeOfTransform(Transform3D transform)
        {
            int chunkSize = 0;
            AddSizeOfTransform(transform, ref chunkSize);

            return chunkSize;
        }

        private void AddSizeOfTransform(Transform3D transform, ref int chunkSize)
        {
            int dataUnitSize = DataPrecision == DataPrecisionType.Double ? SizeOfDouble : SizeOfFloat;

            chunkSize += Chunk.SizeOfChunkHeader;

            if (transform is TranslateTransform3D || transform is ScaleTransform3D)
            {
                chunkSize += dataUnitSize * 3;
            }
            else if (transform is RotateTransform3D)
            {
                chunkSize += dataUnitSize * 7;
            }
            else if (transform is MatrixTransform3D)
            {
                if (!((MatrixTransform3D)transform).Matrix.IsIdentity) // In case of Identity matrix only chunk header is written
                    chunkSize += dataUnitSize * 16;
            }
            else if (transform is Transform3DGroup)
            {
                var transform3DGroup = (Transform3DGroup) transform;

                chunkSize += SizeOfInt32; // number of children

                foreach (var childTransform3D in transform3DGroup.Children)
                    AddSizeOfTransform(childTransform3D, ref chunkSize);
            }
        }

        private int GetSizeOfMeshGeometry3D(MeshGeometry3D mesh)
        {
            int dataUnitSize = DataPrecision == DataPrecisionType.Double ? SizeOfDouble : SizeOfFloat;

            int chunkLength;
            if (DataPrecision == DataPrecisionType.Double)
                chunkLength = SizeOfRect3D; // Bounds
            else
                chunkLength = SizeOfFloatRect3D; // Bounds


            chunkLength += Chunk.SizeOfChunkHeader + mesh.Positions.Count * 3 * dataUnitSize;

            if (SaveNormals && mesh.Normals != null && mesh.Normals.Count > 0)
                chunkLength += Chunk.SizeOfChunkHeader + mesh.Normals.Count * 3 * dataUnitSize;

            if (SaveTextureCoordinates && mesh.TextureCoordinates != null && mesh.TextureCoordinates.Count > 0)
                chunkLength += Chunk.SizeOfChunkHeader + mesh.TextureCoordinates.Count * 2 * dataUnitSize;

            
            if (mesh.TriangleIndices != null && mesh.TriangleIndices.Count > 0)
            {
                if (mesh.Positions.Count < UInt16.MaxValue)
                    chunkLength += Chunk.SizeOfChunkHeader + mesh.TriangleIndices.Count * SizeOfInt16;
                else
                    chunkLength += Chunk.SizeOfChunkHeader + mesh.TriangleIndices.Count * SizeOfInt32;
            }

            var polygonIndices = mesh.GetPolygonIndices();
            if (polygonIndices != null && polygonIndices.Count > 0)
                chunkLength += Chunk.SizeOfChunkHeader + polygonIndices.Count * SizeOfInt32;


            var edgeLineIndices = mesh.GetValue(MeshUtils.EdgeLineIndicesProperty) as Int32Collection;
            if (edgeLineIndices != null && edgeLineIndices.Count > 0)
                chunkLength += Chunk.SizeOfChunkHeader + edgeLineIndices.Count * SizeOfInt32;


            return chunkLength;
        }
        #endregion

        // NOTE: This does not count lights
        private void CollectObjects(Model3D model)
        {
            _modelsCount = 0;
            _totalPositionsCount = 0;
            _totalTriangleIndicesCount = 0;

            _meshesCount    = 0;
            _materialsCount = 0;

            _meshIndexes     = new Dictionary<MeshGeometry3D, int>(100);
            _materialIndexes = new Dictionary<Material, int>(100);

            
            CollectObjectsInt(model);


            _meshes = new MeshGeometry3D[_meshesCount];
            foreach (var keyValuePair in _meshIndexes)
                _meshes[keyValuePair.Value] = keyValuePair.Key;

            _materials = new Material[_materialsCount];
            foreach (var keyValuePair in _materialIndexes)
                _materials[keyValuePair.Value] = keyValuePair.Key;
        }

        private void CollectObjectsInt(Model3D model)
        {
            var geometryModel3D = model as GeometryModel3D;
            if (geometryModel3D != null)
            {
                _modelsCount++;

                var meshGeometry3D = geometryModel3D.Geometry as MeshGeometry3D;
                if (meshGeometry3D != null)
                {
                    // Check if this mesh was already used
                    if (!_meshIndexes.ContainsKey(meshGeometry3D))
                    {
                        _meshIndexes.Add(meshGeometry3D, _meshesCount); // This mesh was not used yet - add it to the dictionary and associate the next index with it
                        _meshesCount++;
                    }

                    if (meshGeometry3D.Positions != null)
                        _totalPositionsCount += meshGeometry3D.Positions.Count;
                    
                    if (meshGeometry3D.TriangleIndices != null)
                        _totalTriangleIndicesCount += meshGeometry3D.TriangleIndices.Count;
                }

                // Do the same for Material
                var material = geometryModel3D.Material;
                if (material != null)
                {
                    // Check if this mesh was already used
                    if (!_materialIndexes.ContainsKey(material))
                    {
                        _materialIndexes.Add(material, _materialsCount);
                        _materialsCount++;
                    }
                }
                
                // ... and for BackMaterial
                material = geometryModel3D.BackMaterial;
                if (material != null)
                {
                    // Check if this mesh was already used
                    if (!_materialIndexes.ContainsKey(material))
                    {
                        _materialIndexes.Add(material, _materialsCount);
                        _materialsCount++;
                    }
                }
            }
            else
            {
                var model3DGroup = model as Model3DGroup;
                if (model3DGroup != null)
                {
                    _modelsCount++;

                    foreach (var oneChild in model3DGroup.Children)
                        CollectObjectsInt(oneChild);
                }
                else
                {
                    // probably a light
                }
            }
        }


        [System.Diagnostics.Conditional("DEBUG")]
        private static void Log(string message)
        {
            if (IsLogging)
                System.Diagnostics.Debug.WriteLine(message);
        }

        private enum ObjectTypes
        {
            Unknown = -1,
            Null = 0,

            Header = 1,

            Comment = 2,

            CameraXaml = 10,

            GeometryModel3D = 100,
            Model3DGroup,

            MeshGeometry3D = 200,
            MeshPositions = 201,
            MeshNormals = 202,
            MeshTextureCoordinates = 203,
            MeshTriangleIndices_16bit = 204,
            MeshTriangleIndices_32bit = 205,
            MeshPolygonIndices = 206,
            MeshEdgeLineIndices = 207,

            DiffuseMaterial = 300,
            SpecularMaterial,
            MaterialGroup,

            SolidColorBrush = 400,
            ImageBrush = 401,

            Transform3DGroup = 500,
            MatrixTransform3D = 501,
            TranslateTransform3D = 502,
            ScaleTransform3D = 503,
            AxisAngleRotateTransform3D = 504,
            QuaternionRotateTransform3D = 505,

            IdentityMatrix3D = 510,

            Bitmap = 600,

            EndOfData = Int32.MaxValue
        }

        private struct Chunk
        {
            public const int SizeOfChunkHeader = 8;

            public readonly BinaryReader Reader;

            private BinaryWriter _writer;

            public readonly ObjectTypes Type;
            public readonly int ChunkLength;

            public readonly long DataStartPosition;
            public readonly long DataEndPosition;

#if DEBUG
            private static Array AllObjectTypesValues;
#endif

            public long ChunkHeaderPosition
            {
                get
                {
                    return DataStartPosition - SizeOfChunkHeader;
                }
            }

            private Chunk(BinaryReader reader)
            {
                Reader = reader;
                _writer = null;

                Type        = (ObjectTypes)reader.ReadInt32();
                ChunkLength = reader.ReadInt32();

                DataStartPosition = (int)reader.BaseStream.Position;
                DataEndPosition = DataStartPosition + ChunkLength;

#if DEBUG
                if (AllObjectTypesValues == null)
                    AllObjectTypesValues = Enum.GetValues(typeof(ObjectTypes));

                if (Array.IndexOf(AllObjectTypesValues, Type) == -1)
                    throw new Exception("Unknown chunk type - int value: " + (int)Type);

                if (ChunkLength < 0 || DataEndPosition > reader.BaseStream.Length)
                    throw new Exception("Invalid chunk length");
#endif
            }

            private Chunk(ObjectTypes chunkType, int chunkLength, BinaryWriter writer)
            {
                _writer = writer;
                Reader = null;

                Type = chunkType;
                ChunkLength = chunkLength;

                DataStartPosition = writer.BaseStream.Position + SizeOfChunkHeader;
                DataEndPosition = DataStartPosition + ChunkLength;
            }

            public static Chunk ReadChunkHeader(BinaryReader reader)
            {
                return new Chunk(reader);
            }
            
            public static Chunk ReadChunkHeader(BinaryReader reader, ObjectTypes expectedChunkType)
            {
                var chunk = new Chunk(reader);

                if (chunk.Type != expectedChunkType)
                    throw new Exception(string.Format("Unexpected chunk type - expected {0} but was {1}", expectedChunkType, chunk.Type));

                return chunk;
            }


            public static Chunk BeginWriteChunk(ObjectTypes chunkType, int chunkLength, BinaryWriter writer)
            {
                Chunk chunk = new Chunk(chunkType, chunkLength, writer);

                if (IsLogging)
                    Log(string.Format("WriteChunkHeader (type, length, Start of chunk pos.):\t{0}\t{1}\t{2}", chunkType, chunkLength, writer.BaseStream.Position));

                writer.Write((int)chunkType);
                writer.Write(chunkLength);

                return chunk;
            }

            // This method skips any unread data from the chunk and positions the reader position to the beginning of the next chunk
            public void EndReadChunk()
            {
                if (Reader.BaseStream.Position > DataEndPosition)
                    throw new FileFormatException("Reading out of chunk bounds!");

                if (Reader.BaseStream.Position != DataEndPosition)
                {
#if DEBUG
                    throw new Exception(string.Format("Chunk {0} at position {1} contains more data then expected (expected: {2}; actual: {3}). This is usually indicator that the file is corrupted", 
                        this.Type, this.DataStartPosition, DataEndPosition  - DataStartPosition, Reader.BaseStream.Position - DataStartPosition));
#else
                    if (IsLogging)
                        Log(string.Format("Skipping {0} bytes", DataEndPosition - Reader.BaseStream.Position));

                    Reader.BaseStream.Seek(DataEndPosition, SeekOrigin.Begin);
#endif
                }
            }

            [Conditional("DEBUG")]
            public void EndWriteChunk()
            { 
                if (_writer.BaseStream.Position != this.DataEndPosition)
                    throw new Exception(string.Format("{0} chunk write error - writer.BaseStream.Position != this.DataEndPosition (diff: {1})", this.Type, _writer.BaseStream.Position - this.DataEndPosition));
            }
        }
    }
}
