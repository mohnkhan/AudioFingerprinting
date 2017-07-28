/**
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.MemoryMappedFiles;

using Lucene.Net.Store;

namespace Lucene.Net.Support
{

   /** File-based {@link Directory} implementation that uses
    *  mmap for reading, and {@link
    *  SimpleFSDirectory.SimpleFSIndexOutput} for writing.
    */
    public class MemoryMappedDirectory : FSDirectory, IDisposable
    {
        public MemoryMappedDirectory(DirectoryInfo dInfo, LockFactory lockFactory)
            : base(dInfo, lockFactory)
        {
        }

        public MemoryMappedDirectory(DirectoryInfo dInfo)
            : base(dInfo, null)
        {
        }

        public MemoryMappedDirectory(string dirName)
            : base(new DirectoryInfo(dirName), null)
        {
        }

        public static MemoryMappedDirectory Open(System.IO.DirectoryInfo path)
        {
            return Open(path, null);
        }

        /// <summary>Just like <see cref="Open(System.IO.DirectoryInfo)" />, but allows you to
        /// also specify a custom <see cref="LockFactory" />. 
        /// </summary>
        public static MemoryMappedDirectory Open(System.IO.DirectoryInfo path, LockFactory lockFactory)
        {               
            return new MemoryMappedDirectory(path, lockFactory);
        }

        /** Creates an IndexInput for the file with the given name. */
        public override IndexInput OpenInput(string name)
        {
            EnsureOpen();
            return new MemoryMappedIndexInput(Path.Combine(internalDirectory.FullName, name));
        }
        public static readonly int DEFAULT_READ_CHUNK_SIZE = Lucene.Net.Util.Constants.JRE_IS_64BIT ? int.MaxValue : 100 * 1024 * 1024;

        // LUCENE-1566
        private int chunkSize = DEFAULT_READ_CHUNK_SIZE;

        /** Creates an IndexInput for the file with the given name. */
        public override IndexInput OpenInput(string name, int bufferSize)
        {
            EnsureOpen();
            try
            {
                return new MemoryMappedIndexInput(Path.Combine(internalDirectory.FullName, name));
            }
            catch (IOException)
            {
                return new SimpleFSDirectory.SimpleFSIndexInput(new FileInfo(Path.Combine(internalDirectory.FullName, name)), BufferedIndexInput.BUFFER_SIZE, chunkSize);
            }
        }

        public override IndexOutput CreateOutput(string name)
        {
            InitOutput(name);
            return new SimpleFSDirectory.SimpleFSIndexOutput(new FileInfo(Path.Combine(internalDirectory.FullName, name)));
        }

        public void Dispose()
        {

        }
    }

    internal class MemoryMappedIndexInput : IndexInput, IDisposable
    {
        MemoryMappedFile _mmf;
        MemoryMappedViewAccessor _accessor;
        long _position = 0;
        private long _length;
        private bool _isClone = false;

        public MemoryMappedIndexInput(string file)
        {
            this._length = new FileInfo(file).Length;

            _mmf = MemoryMappedFile.CreateFromFile(file, FileMode.Open);
            try
            {
                _accessor = _mmf.CreateViewAccessor(0, _length);
            }
            catch (IOException ex)
            {
                _mmf.Dispose();
                throw ex;
            }
        }

        public override byte ReadByte()
        {
            try
            {
                return _accessor.ReadByte(_position++);
            }
            catch (ArgumentException)
            {
                throw new IOException("read past EOF");
            }
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            _position += _accessor.ReadArray(_position, b, offset, len);
        }

        public override long FilePointer
        {
            get
            {
                return _position;
            }
        }

        public override void Seek(long pos)
        {
            _position = pos;
        }

        public override long Length()
        {
            return _length;
        }

        public override object Clone()
        {
            MemoryMappedIndexInput clone = (MemoryMappedIndexInput)base.Clone();
            clone._isClone = true;
            clone._position = 0;
            return clone;
        }

        public void Close()
        {
            if (_isClone) return;
            _accessor.Dispose();
            _mmf.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            this.Close();
        }
    }
}

