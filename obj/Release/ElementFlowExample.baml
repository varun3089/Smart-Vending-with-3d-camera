   M S B A M L   `   `   `  ���� O  Kmscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e0891&clr-namespace:System;assembly=mscorlibSystem  C ?FluidKit, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullG1clr-namespace:FluidKit.Controls;assembly=FluidKitFluidKit.Controls ? ;SVM1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullclr-namespace:testtest    test.ElementFlowExample   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     6sys&clr-namespace:System;assembly=mscorlib     FControls1clr-namespace:FluidKit.Controls;assembly=FluidKit     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006      tclr-namespace:test       � 	Resources    test.StringCollection   
DataSource  ReflectionBrush �� 
StartPoint �� EndPoint � Offset � Color  TestDataTemplate �� DataType ��Name  TestDataTemplate_Reflection � RowDefinitions �� Padding	 J� OpacityMask
 �� ElementName !� Visual �� RelativeTransform �� ScaleX �� ScaleY �� CenterX �� CenterY  ItemTemplate �� CornerRadius��%�  &	        &	      &	 ^     &	 �     7  &	 �      �� $	 0,0&�$	 0,1&�.��� $ 0Q�$ 	#7F000000��� $ .5Q�$ Transparent���� " ��.R��� $ ElementVisual��
������ $��0��._��� $
��Fill=����� �� .R��  �� $
��0.5* ��� $
��0.5* �.+��� $ ElementVisual��$��2q�
�������
���   �$ 2q�._��� $
��Fill=����� �� 8	   $��1������ *ActualHeight+$
 ElementVisual����!�  �� $
 ElementVisual�� �� $ 1Q�$ -1Q�$	 0.5Q�$	 0.5Q��� .R��� ���	#FFB1B1B1$��2q����	#7FFFFFFF$ 0,20,0,0q�$ 3��._��� $
��Left=�$	��Top=�$
��Fill=����� *Image+.��� 
������  �� $��* ��� $
��Auto � � ColumnDefinitions �� $
��0.5* ��� $
��0.5* �.+���  �� Orientation$ Vertical=�$��1��$��0��$��5q�.+��� $��	TiltAngle��$y�Hidden=��� -    ��Name$ _tiltAngleSlider�� � Minimum$ 0Q� � Maximum$ 90Q� � Value$ 0Q�$y�Hidden=��� $��ItemGap��$y�Hidden=��� -   $ _itemGapSlider��$
 0.25Q�$ 3Q�$
 1.35Q�$y�Hidden=��� $ Vertical=�$��1��$��1��$��5q�$y�Hidden=�.+��� $��FrontItemGap��$y�Hidden=��� -   $ _frontItemGapSlider��$ 0Q�$	 4.0Q�$ 3Q�$y�Hidden=��� $��PopoutDistance��$y�Hidden=��� -   $ _popoutDistanceSlider��$
 -2.0Q�$	 2.0Q�$ -1.85Q�$y�Hidden=�#  FluidKit.Controls.ElementFlow  -     Name$ _elementFlow��$��0��$��0��$��2��#���   #���     ElementWidth$	 320Q�   ElementHeight$	 440Q� �� SelectedIndex$ 3��
������    	TiltAngle �� *Value+$
 _tiltAngleSlider��   ItemGap �� *Value+$
 _itemGapSlider��   FrontItemGap �� *Value+$
 _frontItemGapSlider��   PopoutDistance �� *Value+$
 _popoutDistanceSlider��    Layout  !  FluidKit.Controls.CoverFlow  !   Camera! =� " =� FieldOfView$" 60Q�# � Position$# 0,3,61�$ � LookDirection$$ 0,-3,-65�