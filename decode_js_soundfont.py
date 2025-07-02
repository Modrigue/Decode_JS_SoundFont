
import sys
import os
import re
import base64

class Note:
    """Represents a single audio note from a soundfont."""

    def __init__(self, name, data_uri):
        """Initializes a Note object by parsing a data URI."""
        self.name = name
        self.data_uri = data_uri
        self.format = None
        self.value_base64 = None
        self.value = None

        # Parse the data URI to extract format and base64 value
        match = re.match(r'^data:audio/(?P<format>.+);base64,(?P<value_base64>.+)$', data_uri)
        if match:
            self.format = match.group('format')
            self.value_base64 = match.group('value_base64')
            # Decode the base64 string into bytes
            try:
                self.value = base64.b64decode(self.value_base64)
            except base64.binascii.Error as e:
                print(f"Error decoding base64 for note {self.name}: {e}")

    def save(self, dir_path):
        """Saves the decoded audio data to a file."""
        if self.value and self.format:
            file_path = os.path.join(dir_path, f"{self.name}.{self.format}")
            try:
                with open(file_path, 'wb') as f:
                    f.write(self.value)
            except IOError as e:
                print(f"Error saving file {file_path}: {e}")
        else:
            print(f"Cannot save note {self.name} due to missing data or format.")

def remove_line_endings(value):
    """Removes various line ending characters from a string, including unicode separators."""
    if not isinstance(value, str):
        return value
    # Unicode line and paragraph separators
    line_separator = u'\u2028'
    paragraph_separator = u'\u2029'
    return value.replace('\r\n', '').replace('\n', '').replace('\r', '').replace(line_separator, '').replace(paragraph_separator, '')

def process_file(file_path):
    """Processes a single soundfont file to extract and save notes."""
    print(f"Processing {file_path}...")
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()

        # Clean up the file content
        content = remove_line_endings(content)

        # Find the soundfont data block
        match_notes = re.search(r'MIDI\.Soundfont\.(?P<fontName>.+?) = {(?P<jsonBase64>.*)}', content)
        if not match_notes:
            print(f"Soundfont notes not found in file {file_path}")
            return

        font_name = match_notes.group('fontName')
        json_base64 = match_notes.group('jsonBase64')

        # Split the string to find note names and data URIs
        parts = json_base64.split('"')

        notes_list = []
        current_note_name = None

        # Regex patterns for note names and data
        pattern_note_name = re.compile(r'^[A-G]b?[0-8]$')
        pattern_note_data = re.compile(r'^data:audio.*')

        for text in parts:
            if pattern_note_name.match(text):
                current_note_name = text
            elif pattern_note_data.match(text) and current_note_name:
                note = Note(current_note_name, text)
                notes_list.append(note)
                # Reset after pairing to avoid reusing the name
                current_note_name = None

        if not notes_list:
            print(f"No notes found for font '{font_name}' in {file_path}")
            return

        # Create a directory for the soundfont
        dir_path = os.path.dirname(file_path)
        font_dir_path = os.path.join(dir_path, font_name)
        os.makedirs(font_dir_path, exist_ok=True)

        # Save all the notes
        for note in notes_list:
            note.save(font_dir_path)
        print(f"Successfully extracted {len(notes_list)} notes for font '{font_name}'.")

    except Exception as e:
        print(f"Failed to process file {file_path}: {e}")

def main():
    """Main function to handle command-line arguments and start processing."""
    # sys.argv[0] is the script name itself
    args = sys.argv[1:]

    if not args:
        print(f"Usage: python {os.path.basename(__file__)} [file or directory paths]")
        return

    for path in args:
        if os.path.isfile(path):
            process_file(path)
        elif os.path.isdir(path):
            print(f"Processing all files in directory {path}...")
            try:
                for item in os.listdir(path):
                    full_path = os.path.join(path, item)
                    if os.path.isfile(full_path):
                        process_file(full_path)
            except OSError as e:
                print(f"Error reading directory {path}: {e}")
        else:
            print(f"Error: File or directory '{path}' not found.")

if __name__ == "__main__":
    main()
