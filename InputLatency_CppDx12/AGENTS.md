# Application requirements

- Use a content space of exactly 1200 px width * 1300 px height.
- Don't make the application contents responsive at all, except for the background color. This means that it should always render the content to the 1200*1300 content space and then fill the full window with the background color. If the window dimensions cut off content, that is fine.
- Allow window resizing.
- Use only one size of font. No upscaling/downscaling either.
- Use a simple layout flow in which things stack from left to right and top to bottom.
- Display tracking and input diagnostics must use vertical textual lists inside their boxes, with one item per line in the form `Label = value`, such as `Shown frames = value` and `Callback drops = value`.
- Let diagnostics and shortcuts follow the preceding content in the layout flow.
