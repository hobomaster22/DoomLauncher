﻿using System.IO;

namespace DoomLauncher
{
    public class LauncherPath
    {
        private readonly string m_path, m_fullPath;

        public LauncherPath(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                path += Path.DirectorySeparatorChar;

            m_path = path;
            m_fullPath = m_path;

            if (!Path.IsPathRooted(m_fullPath))
            {
                m_fullPath = Path.Combine(Directory.GetCurrentDirectory(), m_fullPath);
            }
            else
            {
                string current = Directory.GetCurrentDirectory();
                if (m_path.StartsWith(current))
                    m_path = m_path.Replace(current, string.Empty);
            }
        }

        public string GetFullPath()
        {
            return m_fullPath;
        }

        public string GetPossiblyRelativePath()
        {
            return m_path;
        } 

        public static implicit operator string(LauncherPath p)
        {
            Util.ThrowDebugException("implicit operator should not be assumed");
            return p.GetFullPath();
        }

        public override bool Equals(object obj)
        {
            LauncherPath path = obj as LauncherPath;
            if (path != null)
                return GetFullPath() == path.GetFullPath();

            return false;
        }

        public override int GetHashCode()
        {
            return GetFullPath().GetHashCode();
        }
    }
}
