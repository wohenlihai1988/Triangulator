using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DemoApp
{
	/// <summary>
	/// A basic demo app for the Triangulator library. This demo triangulates 
	/// a basic diamond shape with a few holes in it.
	/// 
	/// In debug mode, the Triangulator library will create a very verbose
	/// output. This is great for debugging but not so much for performance.
	/// Build the Triangulator library in Release mode or without the DEBUG 
	/// compilation symbol to see a vast increase in performance.
	/// 
	/// Press and hold 'W' to view the shape in wireframe mode.
	/// </summary>
	public class Game1 : Game
	{
		BasicEffect effect;
		VertexDeclaration vertDecl;
		VertexBuffer vertBuffer;
		IndexBuffer indexBuffer;
		int numVertices, numPrimitives;

        RasterizerState wireframe = new RasterizerState 
        {
            CullMode = CullMode.CullCounterClockwiseFace,
            FillMode = FillMode.WireFrame 
        };

		public Game1()
		{
			new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";
			IsMouseVisible = true;
		}

		protected override void LoadContent()
		{
			CreateGeometry();

			effect = new BasicEffect(GraphicsDevice);
			effect.World = Matrix.Identity;
			effect.View = Matrix.CreateLookAt(Vector3.UnitZ, -Vector3.UnitZ, Vector3.Up);
			effect.Projection = Matrix.CreateOrthographicOffCenter(
				-GraphicsDevice.Viewport.Width / 2f,
				GraphicsDevice.Viewport.Width / 2f,
				-GraphicsDevice.Viewport.Height / 2f,
				GraphicsDevice.Viewport.Height / 2f,
				.1f, 10f);
			effect.VertexColorEnabled = true;
			effect.DiffuseColor = new Vector3(1, 0, 0);
		}

		private void CreateGeometry()
		{
			// create or load in some vertices
			Vector2[] sourceVertices = new Vector2[]
			{
				new Vector2(-100, -100),
				new Vector2(0, -200),
				new Vector2(100, -100),
				new Vector2(100, 100),
				new Vector2(0, 200),
				new Vector2(-100, 100)
			};

			// create our hole vertices
			Vector2[] holeVertices = new Vector2[]
			{
				new Vector2(-40, -40),
				new Vector2(-40, 40),
				new Vector2(0, 20),
				new Vector2(40, 40),
				new Vector2(40, -40),
				new Vector2(0, -20),
			};

			// cut the hole out of the source vertices
			sourceVertices = Triangulator.Triangulator.CutHoleInShape(sourceVertices, holeVertices);

			// move the hole up a little bit and cut it out again
			for (int i = 0; i < holeVertices.Length; i++)
				holeVertices[i].Y += 90f;
			sourceVertices = Triangulator.Triangulator.CutHoleInShape(sourceVertices, holeVertices);

			// move it down a bit and cut it out again
			for (int i = 0; i < holeVertices.Length; i++)
				holeVertices[i].Y -= 180f;
			sourceVertices = Triangulator.Triangulator.CutHoleInShape(sourceVertices, holeVertices);

			// create a variable for the indices and triangulate the object
			int[] sourceIndices;
			Triangulator.Triangulator.Triangulate(
				sourceVertices,
				Triangulator.WindingOrder.Clockwise,
				out sourceVertices,
				out sourceIndices);

			// save out some data
			numVertices = sourceVertices.Length;
			numPrimitives = sourceIndices.Length / 3;

			// create the vertex buffer and index buffer using the arrays
			VertexPositionColor[] verts = new VertexPositionColor[sourceVertices.Length];
			for (int i = 0; i < sourceVertices.Length; i++)
				verts[i] = new VertexPositionColor(new Vector3(sourceVertices[i], 0f), Color.White);
			vertBuffer = new VertexBuffer(
				GraphicsDevice, 
                typeof(VertexPositionColor),
                verts.Length * VertexPositionColor.VertexDeclaration.VertexStride,
				BufferUsage.WriteOnly);
			vertBuffer.SetData(verts);

			// branch here to convert our indices to shorts if possible for wider GPU support
			if (verts.Length < 65535)
			{
				short[] indices = new short[sourceIndices.Length];
				for (int i = 0; i < sourceIndices.Length; i++)
					indices[i] = (short)sourceIndices[i];
				indexBuffer = new IndexBuffer(
					GraphicsDevice,
                    IndexElementSize.SixteenBits,
					indices.Length * sizeof(short),
					BufferUsage.WriteOnly);
				indexBuffer.SetData(indices);
			}
			else
			{
				indexBuffer = new IndexBuffer(
					GraphicsDevice,
					IndexElementSize.ThirtyTwoBits,
					sourceIndices.Length * sizeof(int),
					BufferUsage.WriteOnly);
				indexBuffer.SetData(sourceIndices);
			}

            vertDecl = VertexPositionColor.VertexDeclaration;
		}

		protected override void Draw(GameTime gameTime)
		{
			GraphicsDevice.Clear(Color.CornflowerBlue);
            GraphicsDevice.SetVertexBuffer(vertBuffer);
			GraphicsDevice.Indices = indexBuffer;
			

			// if holding 'W' key, render in wireframe
            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                GraphicsDevice.RasterizerState = wireframe;
            }
            else
            {
                GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            }

			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();

				GraphicsDevice.DrawIndexedPrimitives(
					PrimitiveType.TriangleList,
					0,
					0,
					numVertices,
					0,
					numPrimitives);

			}

			base.Draw(gameTime);
		}
	}
}
