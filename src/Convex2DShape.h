#pragma once

#include "ConvexShape.h"

namespace BulletSharp
{
	public ref class Convex2DShape : ConvexShape
	{
	private:
		ConvexShape^ _convexChildShape;

	public:
		Convex2DShape(ConvexShape^ convexChildShape);

		property ConvexShape^ ChildShape
		{
			ConvexShape^ get();
		}
	};
};