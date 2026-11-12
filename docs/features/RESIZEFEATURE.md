## New Feature
In this new feature I want to add resizing functionality using the lines parting the columns in the file explorer tab.

A line is divided into two parts and they should act as one. Ive added two comments on top of said lines in the xaml so
you can find them. Remove those comments when implementing.

Howering over the lines should turn the cursor into that two sided arrow icon. And holding and dragging should resize
the columns of Name, Size, Percent Occupied and Exension depending on which line is dragged.

The columns should have a minimum width. The minimum width should be selected for each column so that its larger than
the content inside at least. For the Name column the size can be smaller than the contents because that column can get
very wide by opening folders.