# SynologyPhotoReorder
This command line application helps me organize the photos on my Synology NAS by importing and reorganizing files into the default folder structure (year\month).

## Usage
On startup you will get these 4 options:
1. Move Files
2. Remove duplicates (with (0) after filename but in same directory)
3. List duplicates (exact same file in different location)
4. ReOrder files (move to correct year\month folder)

### Move Files
Allows you to move files from one directory to the correct structure (year\month) in another directory.

You also have the option to use a third directory to exclude files.

I use this to Import photos to either personal or shared folder but make sure the photos don't already exist in the other folder (shared or personal).

eg: From "D:\Download\PhotosToImport" To "\\{myNas}\photo\Shared\" Exlude "\\{myNas}\home\Photos" will import all photos from PhotosToImport into the shared photo folder, excluding those that already exist in my personal photo folder (filename based!)

### Remove duplicates (with (0) after filename but in same directory)
Iterates all files and checks if another file already exists with the same name but brackets and a number behind it

eg:

Files before
```bash
 - Root
   - img.jpg
   - img (0).jpg
   - img (10).jpg
   - img2.jpg
   - other.jpg
```
would result in
```bash
 - Root
   - img.jpg
   - img2.jpg
   - other.jpg
```


### List duplicates (exact same file in different location)
Iterates all files and checks if another file already exists with the same name and lists both files

### ReOrder files (move to correct year\month folder)
Moves files located in the top level of a directory to subfolders matching year\month the file was created in

## License
[MIT](https://choosealicense.com/licenses/mit/)
