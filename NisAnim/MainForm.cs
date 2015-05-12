﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.IO;

using OpenTK;
using OpenTK.Graphics.OpenGL;

using NisAnim.Conversion;
using NisAnim.IO;
using NisAnim.OpenGL;

namespace NisAnim
{
    public partial class MainForm : Form
    {
        const string oglMarkerName = "marker";
        const string oglEmptyTexture = "empty";
        const string oglDefaultShaderName = "default";

        GLHelper glHelper;
        Matrix4 currentMatrix;
        List<string> glObjectNames;

        BaseFile loadedFile;
        object selectedObj { get { return pgObject.SelectedObject; } }

        bool mouseDown;
        Point mouseCenter, imageOffset;

        Timer timer;
        int animCounter, maxCounter;

        public MainForm()
        {
            InitializeComponent();

            glHelper = new GLHelper(glControl, new Action(Render)) { ClearColor = Color.SkyBlue, };
            glControl.Resize += ((s, e) =>
            {
                glHelper.Viewport = (s as GLControl).ClientRectangle;
            });
            glControl.Load += ((s, e) =>
            {
                glHelper.Textures.AddTexture(oglEmptyTexture, Properties.Resources.Empty);
                glHelper.Shaders.AddProgramWithShaders(oglDefaultShaderName,
                    File.ReadAllText("Data\\Default.vert"),
                    File.ReadAllText("Data\\Default.frag"));

                glHelper.Buffers.AddVertices(oglMarkerName, new GLVertex[]
                {
                    new GLVertex(new Vector3(-300.0f, 0.0f, 0.0f), Vector3.Zero, OpenTK.Graphics.Color4.Black, Vector2.Zero),
                    new GLVertex(new Vector3(300.0f, 0.0f, 0.0f), Vector3.Zero, OpenTK.Graphics.Color4.Black, Vector2.Zero),
                    new GLVertex(new Vector3(0.0f, 300.0f, 0.0f), Vector3.Zero, OpenTK.Graphics.Color4.Black, Vector2.Zero),
                    new GLVertex(new Vector3(0.0f, -300.0f, 0.0f), Vector3.Zero, OpenTK.Graphics.Color4.Black, Vector2.Zero)
                });

                glHelper.Buffers.AddIndices(oglMarkerName, new uint[] { 0, 1, 2, 3 }, PrimitiveType.Lines);
            });

            currentMatrix = Matrix4.Identity;
            glObjectNames = new List<string>();

            SetFormTitle();

            debugDrawToolStripMenuItem.Checked = Properties.Settings.Default.DebugDraw;

            mouseCenter = imageOffset = Point.Empty;

            animCounter = 0;
            maxCounter = 0;

            timer = new Timer();
            timer.Interval = 15;
            timer.Tick += ((s, e) =>
            {
                animCounter++;
                if (animCounter >= maxCounter) animCounter = 0;

                pnlRender.Invalidate();
            });

            timer.Start();

            Application.Idle += ((s, e) =>
            {
                if (glControl.IsIdle)
                    glControl.Invalidate();
            });

            tsslStatus.Text = "Ready";

            // TEMP TEMP TEMP
            // TEMP TEMP TEMP
            pnlRender.Visible = false;
            // TEMP TEMP TEMP
            // TEMP TEMP TEMP
        }

        private void SetFormTitle()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append(Application.ProductName);
            if (loadedFile != null) builder.AppendFormat(" - [{0}]", Path.GetFileName(loadedFile.FilePath));

            Text = builder.ToString();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.LastDat != string.Empty)
            {
                ofdDataFile.InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.LastDat);
                ofdDataFile.FileName = Path.GetFileName(Properties.Settings.Default.LastDat);
            }

            if (ofdDataFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tvObject.Enabled = false;
                pgObject.SelectedObject = null;
                pnlRender.Invalidate();

                ClearObjects();

                BackgroundWorker fileWorker = new BackgroundWorker();
                fileWorker.DoWork += ((s, ev) =>
                {
                    Type fileImplType = null;
                    using (EndianBinaryReader reader = new EndianBinaryReader(File.Open(ofdDataFile.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Endian.BigEndian))
                    {
                        fileImplType = FileHelpers.IdentifyFile(reader, ofdDataFile.FileName);
                    }

                    if (fileImplType == null)
                    {
                        MessageBox.Show("Could not identify file!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        ev.Result = false;
                    }
                    else
                    {
                        loadedFile = (BaseFile)Activator.CreateInstance(fileImplType, new object[] { (Properties.Settings.Default.LastDat = ofdDataFile.FileName) });
                        this.Invoke(new Action(() => { SetFormTitle(); }));

                        ev.Result = true;
                    }
                });

                fileWorker.RunWorkerCompleted += ((s, ev) =>
                {
                    if ((bool)ev.Result == false)
                    {
                        tsslStatus.Text = "Ready";
                        tvObject.Enabled = true;
                        return;
                    }

                    /* TODO have partial treeview updates? */
                    BackgroundWorker treeWorker = new BackgroundWorker();
                    treeWorker.DoWork += ((s2, ev2) =>
                    {
                        tvObject.Invoke(new Action(() => { tvObject.Nodes.Clear(); }));
                        ev2.Result = FileHelpers.TraverseObject(null, Path.GetFileName(loadedFile.FilePath), loadedFile, true);
                    });
                    treeWorker.RunWorkerCompleted += ((s2, ev2) =>
                    {
                        tvObject.Enabled = true;
                        tvObject.Focus();
                        tvObject.Nodes.Add((TreeNode)ev2.Result);

                        tsslStatus.Text = "File loaded";
                    });

                    tsslStatus.Text = "Generating tree...";
                    treeWorker.RunWorkerAsync();
                });

                tsslStatus.Text = "Loading file...";
                fileWorker.RunWorkerAsync();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void debugDrawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.DebugDraw = (sender as ToolStripMenuItem).Checked;
        }

        private void resetTranslationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            imageOffset = Point.Empty;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("NisAnim - NIS Animation Viewer\n\nWritten 2015 by xdaniel - https://github.com/xdanieldzd/", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sfdDataFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (selectedObj is NisPackFile)
                {
                    NisPackFile file = (selectedObj as NisPackFile);
                    file.ParentFile.ExtractFile(file, sfdDataFile.FileName);
                }
                else if (selectedObj is ImageInformation)
                {
                    (selectedObj as ImageInformation).Bitmap.Save(sfdDataFile.FileName);
                }
                else if (selectedObj is SpriteData)
                {
                    (selectedObj as SpriteData).Image.Save(sfdDataFile.FileName);
                }

                sfdDataFile.FileName = Path.GetFileName(sfdDataFile.FileName);
            }
        }

        private void tvObject_AfterSelect(object sender, TreeViewEventArgs e)
        {
            pgObject.SelectedObject = e.Node.Tag;

            ClearObjects();

            if (selectedObj is NisPackFile)
            {
                NisPackFile file = (selectedObj as NisPackFile);

                e.Node.ContextMenuStrip = cmsTreeNode;
                sfdDataFile.Filter = "All Files (*.*)|*.*";
                sfdDataFile.FileName = file.DecompressedFilename;

                if (e.Node.Nodes.Count == 0)
                {
                    if (file.DetectedFileType != null)
                    {
                        string path = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + "_" + file.DecompressedFilename);

                        file.ParentFile.ExtractFile(file, path);
                        object tempObject = (BaseFile)Activator.CreateInstance(file.DetectedFileType, new object[] { path });
                        e.Node.Nodes.Add(FileHelpers.TraverseObject(e.Node, file.DecompressedFilename, tempObject, true));

                        if (File.Exists(path))
                            File.Delete(path);
                    }
                }
            }
            else if (selectedObj is PacFile)
            {
                PacFile file = (selectedObj as PacFile);

                e.Node.ContextMenuStrip = cmsTreeNode;
                sfdDataFile.Filter = "All Files (*.*)|*.*";
                sfdDataFile.FileName = file.Filename;

                if (e.Node.Nodes.Count == 0)
                {
                    if (file.DetectedFileType != null)
                    {
                        string path = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + "_" + file.Filename);

                        file.ParentFile.ExtractFile(file, path);
                        object tempObject = (BaseFile)Activator.CreateInstance(file.DetectedFileType, new object[] { path });
                        e.Node.Nodes.Add(FileHelpers.TraverseObject(e.Node, file.Filename, tempObject, true));

                        if (File.Exists(path))
                            File.Delete(path);
                    }
                }
            }
            else if (selectedObj is ImageInformation)
            {
                e.Node.ContextMenuStrip = cmsTreeNode;
                sfdDataFile.Filter = "Image Files (*.png)|*.png|All Files (*.*)|*.*";

                ImageInformation image = (selectedObj as ImageInformation);
                glObjectNames.Add(image.PrepareRender(glHelper));
            }
            else if (selectedObj is SpriteData)
            {
                e.Node.ContextMenuStrip = cmsTreeNode;
                sfdDataFile.Filter = "Image Files (*.png)|*.png|All Files (*.*)|*.*";

                SpriteData sprite = (selectedObj as SpriteData);
                glObjectNames.Add(sprite.PrepareRender(glHelper));
            }
            else if (selectedObj is ObfPrimitiveListEntry)
            {
                ObfPrimitiveListEntry primitiveListEntry = (selectedObj as ObfPrimitiveListEntry);
                glObjectNames.Add(primitiveListEntry.PrepareRender(glHelper));
            }
            else if (selectedObj is ObfObjectListEntry)
            {
                ObfObjectListEntry objectListEntry = (selectedObj as ObfObjectListEntry);
                glObjectNames.AddRange(objectListEntry.PrepareRender(glHelper));
            }

            pnlRender.Invalidate();

            animCounter = 0;
            maxCounter = 0;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            pnlRender.Invalidate();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.Save();

            if (loadedFile != null)
                loadedFile.Dispose();

            if (glHelper != null)
                glHelper.Dispose();
        }

        private void glControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (glHelper.ProjectionType == ProjectionType.Perspective)
            {
                if (Convert.ToBoolean(glHelper.Camera.MouseButtonsHeld & MouseButtons.Left))
                    glHelper.Camera.MousePosition = e.Location;
            }
            else
            {
                if (mouseDown)
                {
                    imageOffset.X += -(mouseCenter.X - e.X);
                    imageOffset.Y += -(mouseCenter.Y - e.Y);
                    mouseCenter = e.Location;
                }
            }
        }

        private void glControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (glHelper.ProjectionType == ProjectionType.Perspective)
            {
                glHelper.Camera.MouseButtonsHeld &= ~e.Button;
            }
            else
            {
                mouseDown = false;
            }
        }

        private void glControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (glHelper.ProjectionType == ProjectionType.Perspective)
            {
                glHelper.Camera.MouseButtonsHeld |= e.Button;
                if (Convert.ToBoolean(glHelper.Camera.MouseButtonsHeld & MouseButtons.Left)) glHelper.Camera.MousePosition = glHelper.Camera.MouseCenter = e.Location;
            }
            else
            {
                if (e.Button.HasFlag(MouseButtons.Left))
                {
                    mouseDown = true;
                    mouseCenter = e.Location;
                }
            }
        }

        private void glControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (glHelper.ProjectionType == ProjectionType.Perspective)
            {
                glHelper.Camera.KeysHeld.Add(e.KeyCode);
            }
        }

        private void glControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (glHelper.ProjectionType == ProjectionType.Perspective)
            {
                glHelper.Camera.KeysHeld.Remove(e.KeyCode);
            }
        }

        private void glControl_Leave(object sender, EventArgs e)
        {
            if (glHelper.ProjectionType == ProjectionType.Perspective)
            {
                glHelper.Camera.KeysHeld.Clear();
            }
        }

        private void ClearObjects()
        {
            if (glObjectNames.Count == 0) return;

            foreach (string glObjectName in glObjectNames)
            {
                glHelper.Buffers.RemoveVertices(glObjectName);
                glHelper.Buffers.RemoveIndices(glObjectName);
                glHelper.Textures.RemoveTexture(glObjectName);
            }

            glObjectNames.Clear();
        }

        private void Render()
        {
            /* Activate default shader */
            glHelper.Shaders.ActivateProgram(oglDefaultShaderName);

            /* Set projection, modelview, etc. */
            glHelper.ProjectionType = ProjectionType.Orthographic;
            glHelper.ZNear = -10.0f;
            glHelper.ZFar = 10.0f;
            glHelper.Shaders.SetUniformMatrix(oglDefaultShaderName, "projectionMatrix", false, glHelper.CreateProjectionMatrix());
            glHelper.Shaders.SetUniformMatrix(oglDefaultShaderName, "modelviewMatrix", false, Matrix4.CreateTranslation((glHelper.Viewport.Width / 2), (glHelper.Viewport.Height / 2), 0.0f));
            glHelper.Shaders.SetUniformMatrix(oglDefaultShaderName, "objectMatrix", false, Matrix4.Identity);
            glHelper.Shaders.SetUniform(oglDefaultShaderName, "enableLight", 0);

            /* Activate empty dummy texture */
            glHelper.Textures.ActivateTexture(oglEmptyTexture, TextureUnit.Texture0);

            /* Render marker */
            glHelper.Buffers.Render(oglMarkerName);

            if (selectedObj != null)
            {
                if (selectedObj is ObfPrimitiveListEntry || selectedObj is ObfObjectListEntry)
                {
                    glHelper.Camera.Update();

                    glHelper.ProjectionType = ProjectionType.Perspective;
                    glHelper.ZNear = 0.01f;
                    glHelper.ZFar = 1000.0f;

                    glHelper.Shaders.SetUniform(oglDefaultShaderName, "enableLight", 1);
                    glHelper.Shaders.SetUniform(oglDefaultShaderName, "lightPosition", glHelper.Camera.Position);
                    glHelper.Shaders.SetUniform(oglDefaultShaderName, "lightIntensities", new Vector3(1.0f, 1.0f, 1.0f));

                    glHelper.Shaders.SetUniformMatrix(oglDefaultShaderName, "projectionMatrix", false, glHelper.CreateProjectionMatrix());
                    glHelper.Shaders.SetUniformMatrix(oglDefaultShaderName, "modelviewMatrix", false, Matrix4.Identity);
                }
                else
                {
                    glHelper.Shaders.SetUniformMatrix(oglDefaultShaderName, "modelviewMatrix", false, Matrix4.CreateTranslation(imageOffset.X + (glHelper.Viewport.Width / 2), imageOffset.Y + (glHelper.Viewport.Height / 2), 0.0f));
                }

                if (selectedObj is AnimationData)
                {
                    /* Render animation */
                    AnimationData anim = (selectedObj as AnimationData);
                    if (anim.FirstNode != null)
                    {
                        currentMatrix = Matrix4.Identity;
                        RenderAnimationNode(anim.FirstNode);
                    }
                }
                else if (selectedObj is AnimationFrameData)
                {
                    AnimationFrameData animFrame = (selectedObj as AnimationFrameData);
                    currentMatrix = Matrix4.Identity;
                    RenderAnimationFrame(animFrame, Vector2.Zero);
                }
                else if (selectedObj is ImageInformation || selectedObj is SpriteData || selectedObj is ObfPrimitiveListEntry || selectedObj is ObfObjectListEntry)
                {
                    foreach (string glObjectName in glObjectNames)
                    {
                        /* Activate object's texture */
                        glHelper.Textures.ActivateTexture(glObjectName, TextureUnit.Texture0);

                        /* Set matrices */
                        Matrix4 translationMatrix = Matrix4.Identity;
                        if (selectedObj is ImageInformation)
                        {
                            ImageInformation image = (selectedObj as ImageInformation);
                            translationMatrix = Matrix4.CreateTranslation(-(image.Bitmap.Width / 2), -(image.Bitmap.Height / 2), 0.0f);
                        }
                        else if (selectedObj is SpriteData)
                        {
                            SpriteData sprite = (selectedObj as SpriteData);
                            translationMatrix = Matrix4.CreateTranslation(-(sprite.Image.Width / 2), -(sprite.Image.Height / 2), 0.0f);
                        }
                        glHelper.Shaders.SetUniformMatrix(oglDefaultShaderName, "objectMatrix", false, translationMatrix);

                        /* Render object */
                        glHelper.Buffers.Render(glObjectName);
                    }
                }
            }
        }

        private void RenderAnimationNode(AnimationNodeData node)
        {
            Matrix4 lastMatrix = currentMatrix;

            if (node.ChildNode != null)
                RenderAnimationNode(node.ChildNode);

            if (node.FirstAnimationFrameID != -1)
            {
                AnimationFrameData animFrame = node.AnimationFrames.LastOrDefault(x => animCounter >= x.FrameTime);

                maxCounter = Math.Max(maxCounter, node.AnimationFrames.Max(x => x.FrameTime) * 3);

                if (animFrame != null)
                {
                    Vector2 nodeOffset = Vector2.Zero;
                    if ((node.Unknown0x06 & 0x01) == 0x01)
                    {
                        nodeOffset.X = animFrame.Transform.TransformOffset.Offset.X;
                        nodeOffset.Y = animFrame.Transform.TransformOffset.Offset.Y;
                    }
                    else
                    {
                        nodeOffset.X = -animFrame.Transform.TransformOffset.Offset.X;
                        nodeOffset.Y = -animFrame.Transform.TransformOffset.Offset.Y;
                    }

                    RenderAnimationFrame(animFrame, nodeOffset);
                }
            }

            if (node.SiblingNode != null)
                RenderAnimationNode(node.SiblingNode);

            currentMatrix = lastMatrix;
        }

        private void RenderAnimationFrame(AnimationFrameData animFrame, Vector2 offset)
        {
            float scaleX = (animFrame.Transform.Scale.X / 100.0f);
            float scaleY = (animFrame.Transform.Scale.Y / 100.0f);

            if (scaleX == 0.0f || scaleY == 0.0f) return;

            Matrix4 translationMatrix = Matrix4.CreateTranslation((animFrame.Transform.BaseOffset.X + offset.X), (animFrame.Transform.BaseOffset.Y + offset.Y), 0.0f);
            Matrix4 rotationMatrix = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(animFrame.Transform.RotationAngle));
            Matrix4 scaleMatrix = Matrix4.CreateScale(scaleX, scaleY, 1.0f);

            currentMatrix = Matrix4.Mult(translationMatrix, currentMatrix);

            if (animFrame.Unknown0x02 != 1)
                currentMatrix = Matrix4.Mult(Matrix4.CreateTranslation((animFrame.Sprite.Rectangle.Width / 2), (animFrame.Sprite.Rectangle.Height / 2), 0.0f), currentMatrix);

            currentMatrix = Matrix4.Mult(rotationMatrix, currentMatrix);
            currentMatrix = Matrix4.Mult(scaleMatrix, currentMatrix);

            if (animFrame.Unknown0x02 != 1)
                currentMatrix = Matrix4.Mult(Matrix4.CreateTranslation(-(animFrame.Sprite.Rectangle.Width / 2), -(animFrame.Sprite.Rectangle.Height / 2), 0.0f), currentMatrix);

            glHelper.Shaders.SetUniformMatrix(oglDefaultShaderName, "objectMatrix", false, currentMatrix);

            string spriteName = animFrame.Sprite.PrepareRender(glHelper);
            glHelper.Textures.ActivateTexture(spriteName, TextureUnit.Texture0);
            glHelper.Buffers.Render(spriteName);
        }





        private void pnlRender_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.Clear((sender as Control).BackColor);

            e.Graphics.DrawString(imageOffset.ToString(), SystemFonts.CaptionFont, Brushes.Black, Point.Empty);
            e.Graphics.TranslateTransform(e.ClipRectangle.Width / 2, e.ClipRectangle.Height / 2);
            e.Graphics.TranslateTransform(imageOffset.X, imageOffset.Y);

            e.Graphics.DrawLine(Pens.Black, -300, 0, 300, 0);
            e.Graphics.DrawLine(Pens.Black, 0, -300, 0, 300);

            /* TODO move to individual txf and anmdat classes? */
            if (selectedObj is ImageInformation)
            {
                Bitmap image = (selectedObj as ImageInformation).Bitmap;
                e.Graphics.DrawImage(image, new Point(-(image.Width / 2), -(image.Height / 2)));
            }
            else if (selectedObj is AnimationData)
            {
                AnimationData anim = (selectedObj as AnimationData);
                if (anim.FirstNode != null) DrawAnimationNode(e.Graphics, anim.FirstNode);
            }
            else if (selectedObj is AnimationFrameData)
            {
                DrawAnimationFrame(e.Graphics, (selectedObj as AnimationFrameData), Point.Empty);
            }
            else if (selectedObj is SpriteData)
            {
                SpriteData sprite = (selectedObj as SpriteData);
                e.Graphics.DrawImage(sprite.Image, new Point(-(sprite.Image.Width / 2), -(sprite.Image.Height / 2)));
            }

            e.Graphics.ResetTransform();
        }

        private void DrawAnimationNode(Graphics g, AnimationNodeData node)
        {
            Matrix prevTransform = g.Transform;

            if (node.ChildNode != null)
                DrawAnimationNode(g, node.ChildNode);

            if (node.FirstAnimationFrameID != -1)
            {
                AnimationFrameData animFrame = node.AnimationFrames.LastOrDefault(x => animCounter >= x.FrameTime);

                maxCounter = Math.Max(maxCounter, node.AnimationFrames.Max(x => x.FrameTime) * 3);

                if (animFrame != null)
                {
                    //dunno, probably not...
                    //if (animFrame.Unknown0x02 == 1) g.Transform = prevTransform;

                    Point nodeOffset = Point.Empty;
                    if ((node.Unknown0x06 & 0x01) == 0x01)
                    {
                        nodeOffset.X = animFrame.Transform.TransformOffset.Offset.X;
                        nodeOffset.Y = animFrame.Transform.TransformOffset.Offset.Y;
                    }
                    else
                    {
                        nodeOffset.X = -animFrame.Transform.TransformOffset.Offset.X;
                        nodeOffset.Y = -animFrame.Transform.TransformOffset.Offset.Y;
                    }

                    DrawAnimationFrame(g, animFrame, nodeOffset);
                }
            }

            if (node.SiblingNode != null)
                DrawAnimationNode(g, node.SiblingNode);

            g.Transform = prevTransform;
        }

        private void DrawAnimationFrame(Graphics g, AnimationFrameData animFrame, Point offset)
        {
            /* 09016 title screen */
            /* 00005 senate */
            /* 00050 desco battle */
            /* 24001 fuka convo A */

            float scaleX = (animFrame.Transform.Scale.X / 100.0f);
            float scaleY = (animFrame.Transform.Scale.Y / 100.0f);

            if (scaleX == 0.0f || scaleY == 0.0f) return;

            Point framePosition = new Point(animFrame.Transform.BaseOffset.X + offset.X, animFrame.Transform.BaseOffset.Y + offset.Y);

            /* likely wrong? */
            //if (animFrame.Sprite.Rectangle.Width == 0) framePosition.X += (animFrame.Sprite.Rectangle.X + animFrame.Sprite.Rectangle.Y) / 2;
            //if (animFrame.Sprite.Rectangle.Height == 0) framePosition.Y += (animFrame.Sprite.Rectangle.X + animFrame.Sprite.Rectangle.Y) / 2;

            g.TranslateTransform(framePosition.X, framePosition.Y);

            if (animFrame.Unknown0x02 != 1)
                g.TranslateTransform(animFrame.Sprite.Rectangle.Width / 2, animFrame.Sprite.Rectangle.Height / 2);
            g.RotateTransform(animFrame.Transform.RotationAngle);
            g.ScaleTransform(scaleX, scaleY);
            if (animFrame.Unknown0x02 != 1)
                g.TranslateTransform(-(animFrame.Sprite.Rectangle.Width / 2), -(animFrame.Sprite.Rectangle.Height / 2));

            g.DrawImage(animFrame.Sprite.Image, Point.Empty);

            if (Properties.Settings.Default.DebugDraw)
            {
                Pen debugRectPen = ((animFrame.Transform.RotationAngle != 0 || scaleX != 1.0f || scaleY != 1.0f) ? Pens.OrangeRed : Pens.Yellow);
                g.DrawRectangle(debugRectPen, new Rectangle(0, 0, animFrame.Sprite.Rectangle.Width, animFrame.Sprite.Rectangle.Height));
                g.DrawString(string.Format("{{X={0}, Y={1}}}", framePosition.X, framePosition.Y), SystemFonts.StatusFont, Brushes.Blue, Point.Empty);
            }
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            timer.Start();
        }

        private void MainForm_Deactivate(object sender, EventArgs e)
        {
            timer.Stop();
        }
    }
}
